
using System;
using System.Threading;
using System.Threading.Tasks;
using Proto.Mailbox;

namespace Boost.Proto.Actor.Opentelemetry;

public static class UnboundedOpenTelemetryMailbox
{
    public static IMailbox Create(params IMailboxStatistics[] stats) =>
        new OpenTelemetryDefaultMailbox(new LockingUnboundedMailboxQueue(4), new UnboundedMailboxQueue(), stats);
}


public sealed class OpenTelemetryDefaultMailbox : IMailbox
#if NET5_0_OR_GREATER
, IThreadPoolWorkItem
#endif
{
    class NoopDispatcher : IDispatcher
    {
        internal static readonly IDispatcher Instance = new NoopDispatcher();
        public int Throughput => 0;

        public void Schedule(Func<Task> runner) => throw new NotImplementedException();
    }

    class NoopInvoker : IMessageInvoker
    {
        internal static readonly IMessageInvoker Instance = new NoopInvoker();

        public CancellationTokenSource CancellationTokenSource => throw new NotImplementedException();

        public ValueTask InvokeSystemMessageAsync(SystemMessage msg) => throw new NotImplementedException();

        public ValueTask InvokeUserMessageAsync(object msg) => throw new NotImplementedException();

        public void EscalateFailure(Exception reason, object? message) => throw new NotImplementedException();
    }

    static class MailboxStatus
    {
        public const int Idle = 0;
        public const int Busy = 1;
    }

    private readonly IMailboxStatistics[] _stats;
    private readonly IMailboxQueue _systemMessages;
    private readonly IMailboxQueue _userMailbox;
    private IDispatcher _dispatcher;
    private IMessageInvoker _invoker;

    private long _status = MailboxStatus.Idle;
    private bool _suspended;

    public OpenTelemetryDefaultMailbox(
        IMailboxQueue systemMessages,
        IMailboxQueue userMailbox
    )
    {
        _systemMessages = systemMessages;
        _userMailbox = userMailbox;
        _stats = Array.Empty<IMailboxStatistics>();

        _dispatcher = NoopDispatcher.Instance;
        _invoker = NoopInvoker.Instance;
    }

    public OpenTelemetryDefaultMailbox(
        IMailboxQueue systemMessages,
        IMailboxQueue userMailbox,
        params IMailboxStatistics[] stats
    )
    {
        _systemMessages = systemMessages;
        _userMailbox = userMailbox;
        _stats = stats;

        _dispatcher = NoopDispatcher.Instance;
        _invoker = NoopInvoker.Instance;
    }

    public int Status => (int) Interlocked.Read(ref _status);

    public int UserMessageCount => _userMailbox.Length;

    public void PostUserMessage(object msg)
    {
        // if the message is a batch message, we unpack the content as individual messages in the mailbox
        // feature Aka: Samkuvertering in Swedish...
        if (msg is IMessageBatch || msg is MessageEnvelope e && e.Message is IMessageBatch)
        {
            var batch = (IMessageBatch) MessageEnvelope.UnwrapMessage(msg)!;
            var messages = batch.GetMessages();

            foreach (var message in messages)
            {
                _userMailbox.Push(message);

                foreach (var t in _stats)
                {
                    t.MessagePosted(message);
                }
            }

            if (batch is IAutoRespond)
            {
                // push the batch itself as well, so that it can autorespond to sender after processing all contained messages
                // used by pub sub
                _userMailbox.Push(msg);
            }

            foreach (var t in _stats)
            {
                t.MessagePosted(msg);
            }

            Schedule();
        }
        else
        {
            _userMailbox.Push(msg);

            foreach (var t in _stats)
            {
                t.MessagePosted(msg);
            }

            Schedule();
        }
    }

    public void PostSystemMessage(object msg)
    {
        _systemMessages.Push(msg);

        if (msg is Stop)
            _invoker?.CancellationTokenSource?.Cancel();

        foreach (var t in _stats)
        {
            t.MessagePosted(msg);
        }

        Schedule();
    }

    public void RegisterHandlers(IMessageInvoker invoker, IDispatcher dispatcher)
    {
        _invoker = invoker;
        _dispatcher = dispatcher;
    }

    public void Start()
    {
        foreach (var t in _stats)
        {
            t.MailboxStarted();
        }
    }

    private static Task RunAsync(OpenTelemetryDefaultMailbox mailbox)
    {
        var task = mailbox.ProcessMessages();

        if (!task.IsCompletedSuccessfully)
        {
            return Await(mailbox, task);
        }

        Interlocked.Exchange(ref mailbox._status, MailboxStatus.Idle);

        if (mailbox._systemMessages.HasMessages || !mailbox._suspended && mailbox._userMailbox.HasMessages)
        {
            mailbox.Schedule();
        }
        else
        {
            foreach (var t in mailbox._stats)
            {
                t.MailboxEmpty();
            }
        }

        return Task.CompletedTask;

        static async Task Await(OpenTelemetryDefaultMailbox self, ValueTask task)
        {
            await task;

            Interlocked.Exchange(ref self._status, MailboxStatus.Idle);

            if (self._systemMessages.HasMessages || !self._suspended && self._userMailbox.HasMessages)
            {
                self.Schedule();
            }
            else
            {
                foreach (var t in self._stats)
                {
                    t.MailboxEmpty();
                }
            }
        }
    }

    private ValueTask ProcessMessages()
    {
        object? msg = null;

        try
        {
            for (var i = 0; i < _dispatcher.Throughput; i++)
            {
                msg = _systemMessages.Pop();

                if (msg is SystemMessage sys)
                {
                    var extra = MessageEnvelope.Wrap(sys);



                    _suspended = msg switch
                    {
                        SuspendMailbox => true,
                        ResumeMailbox  => false,
                        _              => _suspended
                    };

                    var t = _invoker.InvokeSystemMessageAsync(sys);

                    if (!t.IsCompletedSuccessfully)
                    {
                        return Await(msg, t, this);
                    }

                    foreach (var t1 in _stats)
                    {
                        t1.MessageReceived(msg);
                    }

                    continue;
                }

                if (_suspended) break;

                msg = _userMailbox.Pop();

                if (msg is not null)
                {
                    var t = _invoker.InvokeUserMessageAsync(msg);

                    if (!t.IsCompletedSuccessfully)
                    {
                        return Await(msg, t, this);
                    }

                    foreach (var t1 in _stats)
                    {
                        t1.MessageReceived(msg);
                    }
                }
                else
                    break;
            }
        }
        catch (Exception e)
        {
            e.CheckFailFast();
            _invoker.EscalateFailure(e, msg);
        }

        return default;

        static async ValueTask Await(object msg, ValueTask task, OpenTelemetryDefaultMailbox self)
        {
            try
            {
                await task;

                foreach (var t1 in self._stats)
                {
                    t1.MessageReceived(msg);
                }
            }
            catch (Exception e)
            {
                e.CheckFailFast();
                self._invoker.EscalateFailure(e, msg);
            }
        }
    }

    private void Schedule()
    {
        if (Interlocked.CompareExchange(ref _status, MailboxStatus.Busy, MailboxStatus.Idle) == MailboxStatus.Idle)
        {
#if NET5_0_OR_GREATER
            ThreadPool.UnsafeQueueUserWorkItem(this, false);
#else
            ThreadPool.UnsafeQueueUserWorkItem(RunWrapper, this);
#endif
        }
    }

#if NET5_0_OR_GREATER
    public void Execute()
    {
        _ = RunAsync(this);
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RunWrapper(object state)
    {
        var y = (DefaultMailbox) state;
        RunAsync(y);
    }
#endif
}
