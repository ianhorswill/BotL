using System;
using System.Collections;
using System.Text;
using BotL.Compiler;
using static BotL.KB;
using static BotL.Engine;

namespace BotL
{
    /// <summary>
    /// A queue that retains data after it's dequeued, so that dequeue operations can be backtracked.
    /// </summary>
    public class Queue : IList
    {
        private object[] data = new object[8];
        // Next position to remove
        private int head;
        // Next position in which to add
        private int tail;

        enum MatchMode
        {
            Parse,
            Generate
        }

        private MatchMode mode = MatchMode.Parse;

        public int Count => tail - head;

        public void Clear()
        {
            head = tail = 0;
        }

        public void Enqueue(object o)
        {
            if (tail == data.Length-1)
            {
                var newData = new object[data.Length * 2];
                Array.Copy(data, newData, data.Length);
                data = newData;
            }
            data[tail++] = o;
        }

        public object Dequeue()
        {
            if (head == tail)
            {
                throw new InvalidOperationException("dequeue from empty queue");
            }
            return data[head++];
        }

        #region IList stuff
        public int Add(object o)
        {
            Enqueue(o);
            return Count - 1;
        }

        public object this[int i]
        {
            get { return data[head + i]; }
            set { data[head + i] = value; }
        }

        public int IndexOf(object o)
        {
            for (int i = head; i < tail; i++)
                if (Equals(data[i], o))
                    return i - head;
            return -1;
        }

        public bool IsFixedSize => false;
        public bool IsReadOnly => false;
        public bool IsSynchronized => false;
        public object SyncRoot => data.SyncRoot;

        public IEnumerator GetEnumerator()
        {
            for (int i = head; i < tail; i++)
                yield return data[i];
        }

        public void CopyTo(Array a, int l)
        {
            Array.Copy(data, head, a, l, Count);
        }

        public bool Contains(object o)
        {
            return IndexOf(o) >= 0;
        }

        public void Insert(int i, object o)
        {
            throw new NotImplementedException();
        }

        public void Remove(object o)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int i)
        {
            throw new NotImplementedException();
        }
        #endregion

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("queue(");
            var isFirst = true;
            for (int i = head; i < tail; i++)
            {
                if (isFirst)
                    isFirst = false;
                else
                    b.Append(", ");
                b.Append(data[i]);
            }
            return base.ToString();
        }

        internal static void DefineQueuePrimops()
        {
            // Nonbacktrackable enqueue
            DefinePrimop("enqueue", 2, (argBase, ignore) =>
            {
                var qAddr = Deref(argBase);
                var queue = DataStack[qAddr].reference as Queue;
                if (queue == null || DataStack[qAddr].Type != TaggedValueType.Reference)
                    throw new ArgumentTypeException("enqueue", 1, "should be a queue", DataStack[qAddr].Value);
                var vAddr = Deref(argBase + 1);
                if (DataStack[vAddr].Type == TaggedValueType.Unbound)
                    throw new InstantiationException("enqueue: second argument must be instantiated.");
                queue.Enqueue(DataStack[vAddr].Value);
                return CallStatus.DeterministicSuccess;
            });

            // Nonbacktrackable dequeue
            DefinePrimop("dequeue", 2, (argBase, ignore) =>
            {
                var qAddr = Deref(argBase);
                var queue = DataStack[qAddr].reference as Queue;
                if (queue == null || DataStack[qAddr].Type != TaggedValueType.Reference)
                    throw new ArgumentTypeException("dequeue", 1, "should be a queue", DataStack[qAddr].Value);
                var value = queue.Dequeue();
                var vAddr = Deref(argBase + 1);
                if (DataStack[vAddr].Type == TaggedValueType.Unbound)
                {
                    DataStack[vAddr].SetGeneral(value);
                    return CallStatus.DeterministicSuccess;
                }
                return DataStack[vAddr].EqualGeneral(value) ? CallStatus.DeterministicSuccess : CallStatus.Fail;
            });
            // Dequeue can also be called as a function
            Functions.DeclareFunction("dequeue", 1);

            DefinePrimop("word", 2, (argBase, ignore) =>
            {
                var qAddr = Deref(argBase + 1);
                var queue = DataStack[qAddr].reference as Queue;
                if (queue == null || DataStack[qAddr].Type != TaggedValueType.Reference)
                    throw new ArgumentTypeException("word", 2, "should be a queue", DataStack[qAddr].Value);
                var vAddr = Deref(argBase);
                switch (queue.mode)
                {
                    case MatchMode.Parse:
                    {
                        if (queue.Count == 0)
                            return CallStatus.Fail;
                        UndoStack[uTop++].Set(RestoreHead, queue, queue.head);
                        var value = queue.Dequeue();

                        if (DataStack[vAddr].Type == TaggedValueType.Unbound)
                        {
                            DataStack[vAddr].SetGeneral(value);
                            SaveVariable(vAddr);
                            return CallStatus.DeterministicSuccess;
                        }
                        return DataStack[vAddr].EqualGeneral(value) ? CallStatus.DeterministicSuccess : CallStatus.Fail;
                    }
                        
                    case MatchMode.Generate:
                    {
                        // Generate mode
                        UndoStack[uTop++].Set(RestoreTail, queue, queue.tail);
                        if (DataStack[vAddr].Type == TaggedValueType.Unbound)
                            throw new InstantiationException("Attempt to write uninstantiated variable to queue");
                        queue.Enqueue(DataStack[vAddr].Value);
                        return CallStatus.DeterministicSuccess;
                    }


                    default:
                        throw new InvalidOperationException("Bad match mode in queue");
                }
            });

            DefinePrimop("words", 2, (argBase, ignore) =>
            {
                var qAddr = Deref(argBase + 1);
                var queue = DataStack[qAddr].reference as Queue;
                if (queue == null || DataStack[qAddr].Type != TaggedValueType.Reference)
                    throw new ArgumentTypeException("words", 2, "should be a queue", DataStack[qAddr].Value);
                var vAddr = Deref(argBase);
                object[] seq = DataStack[vAddr].reference as object[];
                if (DataStack[vAddr].Type != TaggedValueType.Reference || seq == null)
                    throw new ArgumentTypeException("words", 1, "Should be an array",
                        DataStack[vAddr].Value);

                switch (queue.mode)
                {
                    case MatchMode.Parse:
                    {
                        if (queue.Count < seq.Length)
                            return CallStatus.Fail;
                        UndoStack[uTop++].Set(RestoreHead, queue, queue.head);

                        foreach (var word in seq)
                            if (!Equals(word, queue.Dequeue()))
                                return CallStatus.Fail;
                        return CallStatus.DeterministicSuccess;
                    }
                    case MatchMode.Generate:
                    {
                        // Generate mode
                        UndoStack[uTop++].Set(RestoreTail, queue, queue.tail);
                        foreach (var word in seq)
                            queue.Enqueue(word);
                        return CallStatus.DeterministicSuccess;
                    }

                    default:
                        throw new InvalidOperationException("Bad match_mode in queue");
                }
            });

            DefinePrimop("set_generate_mode", 1, (argBase, ignore) =>
            {
                var qAddr = Deref(argBase);
                var queue = DataStack[qAddr].reference as Queue;
                if (queue == null || DataStack[qAddr].Type != TaggedValueType.Reference)
                    throw new ArgumentTypeException("set_generate_mode", 1, "should be a queue", DataStack[qAddr].Value);
                queue.mode = MatchMode.Generate;
                return CallStatus.DeterministicSuccess;
            });
        }

        private static void RestoreHead(ref UndoRecord arg)
        {
            ((Queue) arg.objArg).head = arg.intArg;
        }
        private static void RestoreTail(ref UndoRecord arg)
        {
            ((Queue)arg.objArg).tail = arg.intArg;
        }
    }
}
