using System;

namespace BotL
{
    /// <summary>
    /// A closure to be called to undo some state change when backtracking 
    /// </summary>
    /// <param name="arg">The UndoRecord containing further information about what to undo.</param>
    delegate void UndoAction(ref UndoRecord arg);

    /// <summary>
    /// A record indicating some action to take to undo some state chance when the system backtracks.
    /// </summary>
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
