using System;

namespace BotL
{
    delegate void UndoAction(ref UndoRecord arg);
    internal struct UndoRecord
    {
        public UndoAction Action;
        public object objArg;
        public int intArg;
        public TaggedValue TaggedArg;

        public void Invoke()
        {
            Action(ref this);
        }

        public void Set(UndoAction a)
        {
            Action = a;
        }

        public void Set(UndoAction a, object o, int i)
        {
            Action = a;
            objArg = o;
            intArg = i;
        }

        public void Set(UndoAction a, object o, ref TaggedValue t)
        {
            Action = a;
            objArg = o;
            TaggedArg = t;
        }
    }
}
