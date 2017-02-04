﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using BotL.Compiler;
using static BotL.Engine;

namespace BotL
{
    /// <summary>
    /// A node in the exclusion logic/eremic logic database
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public sealed class ELNode
    {
        public static readonly ELNode Root;

        static ELNode()
        {
            Root = new ELNode(null);
            Root.Key.SetReference("Root");
        }

        #region Allocation and deallocation
        private static readonly Stack<ELNode> NodePool = new Stack<ELNode>();

        /// <summary>
        /// Returns an unused node, either by takign it from the pool, or by constructing a new one.
        /// </summary>
        private static ELNode Allocate(ref TaggedValue key, ELNode nextSibling, ELNode parent)
        {
            if (NodePool.Count == 0)
                return new ELNode(ref key, nextSibling, parent);
            var node = NodePool.Pop();
            node.Key = key;
            node.keyHash = key.Hash();
            node.previousSibling = null;
            node.NextSibling = nextSibling;
            node.parent = parent;
            node.IsExclusive = false;
            return node;
        }

        /// <summary>
        /// If you aren't implementing the internals of the system, you probably mean to call Delete().
        /// 
        /// Deallocate() returns node and its descendants to the pool.
        /// Does not unlink it from its parent or siblings.
        /// </summary>
        private void Deallocate()
        {
            var c = FirstChild;
            while (c != null)
            {
                c.Deallocate();
                c = c.NextSibling;
            }
            NodePool.Push(this);
        }

        private ELNode(ELNode parent)
        {
            this.parent = parent;
        }

        private ELNode(ref TaggedValue key, ELNode nextSibling, ELNode parent)
        {
            Key = key;
            keyHash = key.Hash();
            NextSibling = nextSibling;
            this.parent = parent;
        }
        #endregion

        #region Instance fields
        internal bool IsExclusive;
        private ELNode parent;
        // ReSharper disable once MemberCanBePrivate.Global
        public TaggedValue Key;
        private int keyHash;

        internal ELNode NextSibling;
        private ELNode previousSibling;
        internal ELNode FirstChild;
        #endregion

        /// <summary>
        /// Find a child matching the specified key.
        /// </summary>
        /// <param name="keyToMatch">Key to match</param>
        /// <returns>Matching child or null, if no matching child.</returns>
        private ELNode FindChild(ref TaggedValue keyToMatch)
        {
            var h = keyToMatch.Hash();
            for (var c = FirstChild; c != null; c = c.NextSibling)
                if (c.keyHash == h && c.Key.Match(ref keyToMatch))
                    return c;
            return null;
        }

        /// <summary>
        /// Removes a node from the database and returns its storage to the pool.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public void Delete()
        {
            if (previousSibling == null)
                // We're first in the list
                parent.FirstChild = NextSibling;
            else
                previousSibling.NextSibling = NextSibling;
            if (NextSibling != null)
            {
                NextSibling.previousSibling = previousSibling;
            }
            // Node is now delinked
            Deallocate();
        }

        #region Primops
        internal static void DefineELPrimops()
        {
            KB.DefinePrimop("read_nonexclusive", 3, (argBase, restartCount) => ReadEL(argBase, restartCount, false));
            KB.DefinePrimop("read_exclusive", 3, (argBase, restartCount) => ReadEL(argBase, restartCount, true));
            KB.DefinePrimop("write_nonexclusive", 3, (argBase, restartCount) => WriteEL(argBase, false));
            KB.DefinePrimop("write_exclusive", 3, (argBase, restartCount) => WriteEL(argBase, true));
            KB.DefinePrimop("delete_el_node", 1, (argBase, ignore) =>
            {
                ((ELNode)DataStack[Deref(argBase)].reference).Delete();
                return CallStatus.DeterministicSuccess;
            });
        }

        private static ELNode DecodeNodeArg(ushort stackAddress)
        {
            var deref = Deref(stackAddress);
            var nodeArg = DataStack[deref].reference;
            if (nodeArg == null)
                return Root;

            var node = nodeArg as ELNode;
            if (DataStack[deref].Type != TaggedValueType.Reference)
            {
                if (DataStack[deref].Type == TaggedValueType.Unbound)
                    throw new InstantiationException("ELNode argument must be instantiated");
                throw new ArgumentException("Argument is not an ELNode: "+DataStack[deref].Value);
            }
            return node;
        }

        // ReSharper disable once InconsistentNaming
        private static CallStatus ReadEL(ushort argBase, ushort restartCount, bool isExclusive)
        {
            var node = DecodeNodeArg(argBase);
            if (node.IsExclusive != isExclusive)
            {
                if (isExclusive)
                    throw new ArgumentException("Exclusive read of non-exclusive EL node "+node);
                throw new ArgumentException("Non-exclusive read of exclusive EL node " + node);
            }
            var keyAddr = Deref(argBase + 1);
            var resultAddr = Deref(argBase + 2);
            if (DataStack[keyAddr].Type == TaggedValueType.Unbound)
            {
                // We're enumerating nodes
                ELNode child;
                if (restartCount == 0)
                {
                    // First time through
                    child = node.FirstChild;
                    if (child == null)
                        return CallStatus.Fail;
                }
                else
                {
                    // We know DataStack[resultAddr] previously had the previous sibling.
                    // When it was unbound, the Type was reset, but the pointer is still
                    // sitting in the reference field.
                    child = ((ELNode) DataStack[resultAddr].reference).NextSibling;
                }
                DataStack[keyAddr] = child.Key;
                DataStack[resultAddr].SetReference(child);
                SaveUndo(keyAddr);
                SaveUndo(resultAddr);
                return child.NextSibling == null
                    ? CallStatus.DeterministicSuccess
                    : CallStatus.NonDeterministicSuccess;
            }

            // Second arg is bound, so we're matching to it
            var match = node.FindChild(ref DataStack[keyAddr]);
            if (match == null)
                return CallStatus.Fail;
            DataStack[resultAddr].SetReference(match);
            SaveUndo(resultAddr);
            return CallStatus.DeterministicSuccess;
        }

        private static CallStatus WriteEL(ushort argBase, bool isExclusive)
        {
            var node = DecodeNodeArg(argBase);
            var keyAddr = Deref(argBase + 1);
            var resultAddr = Deref(argBase + 2);
            if (DataStack[keyAddr].Type == TaggedValueType.Unbound)
                throw new InstantiationException("Attempt to write uninstantiated variable to EL database");
            if (isExclusive != node.IsExclusive && node.FirstChild != null)
                throw new InvalidOperationException(isExclusive?"Exclsive write to non-exclusive EL node":"Non-exclusive write to exclusive EL node");
            var result = node.FindChild(ref DataStack[keyAddr]);
            if (result == null)
            {
                // It's not there; add it.
                if (isExclusive)
                {
                    if (node.FirstChild == null)
                    {
                        // Make child node.
                        result = node.FirstChild = Allocate(ref DataStack[keyAddr], null, node);
                        node.IsExclusive = true;
                    }
                    else
                    {
                        // Overwrite existing node.
                        result = node.FirstChild;
                        if (!result.Key.Match(ref DataStack[keyAddr]))
                        {
                            // This is an actual change in the node.
                            result.Key = DataStack[keyAddr];
                            result.keyHash = result.Key.Hash();
                            // Deallocate any existing children
                            if (result.FirstChild != null)
                            {
                                result.FirstChild.Deallocate();
                                result.FirstChild = null;
                            }
                        }
                    }
                }
                else
                {
                    // Add a new node.
                    result = node.FirstChild = Allocate(ref DataStack[keyAddr], node.FirstChild, node);
                    if (result.NextSibling != null)
                        result.NextSibling.previousSibling = result;
                }
            }
            DataStack[resultAddr].SetReference(result);
            SaveUndo(resultAddr);
            return CallStatus.DeterministicSuccess;   
        }
        #endregion

        #region Macros

        private static readonly Symbol StoreNode = Symbol.Intern(">>");
        internal static void DefineElMacros()
        {
            Macros.DeclareMacro("/", 2, (left, right) => ExpandEL(new Call(Symbol.Slash, left, right)));
            Macros.DeclareMacro(":", 2, (left, right) => ExpandEL(new Call(Symbol.Colon, left, right)));
            Macros.DeclareMacro(">>", 2, (left, right) => ExpandEL(new Call(StoreNode, left, right)));
        }

        private static object ExpandEL(object exp)
        {
            return ExpandEL(exp, Symbol.Underscore);
        }

        private static object ExpandEL(object exp, object resultVar)
        {
            var c = exp as Call;
            if (c == null)
                throw new SyntaxError("Invalid exclusion logic expression", exp);
            if (c.IsFunctor(Symbol.Slash, 1))
            {
                return new Call("read_nonexclusive", null, c.Arguments[0], resultVar);
            }
            if (c.IsFunctor(Symbol.Slash, 2))
            {
                if (c.Arguments[0] is Symbol)
                    return new Call("read_nonexclusive", c.Arguments[0], c.Arguments[1], resultVar);
                var temp = Variable.MakeGenerated("*Node*");
                var parentCode = ExpandEL(c.Arguments[0], temp);
                return Macros.And(parentCode, new Call("read_nonexclusive", temp, c.Arguments[1], resultVar));
            }
            if (c.IsFunctor(Symbol.Colon, 2))
            {
                if (c.Arguments[0] is Symbol)
                    return new Call("read_exclusive", c.Arguments[0], c.Arguments[1], resultVar);
                var temp = Variable.MakeGenerated("*Node*");
                var parentCode = ExpandEL(c.Arguments[0], temp);
                return Macros.And(parentCode, new Call("read_exclusive", temp, c.Arguments[1], resultVar));
            }
            if (c.IsFunctor(StoreNode, 2))
            {
                return ExpandEL(c.Arguments[0], c.Arguments[1]);
            }
            throw new SyntaxError("Invalid exclusion logic expression", exp);
        }

        public static object ExpandUpdate(Symbol updateOperation, object elExpr)
        {
            if (updateOperation.Name == "assert_internal")
                return ExpandWrite(elExpr, Symbol.Underscore);
            if (updateOperation.Name == "retract_internal")
            {
                var node = Variable.MakeGenerated("*Node*");
                var lookup = ExpandEL(elExpr, node);
                return Macros.And(lookup, new Call("delete_el_node", node));
            }
            throw new InvalidOperationException($"Cannot perform {updateOperation} on EL database");
        }

        private static object ExpandWrite(object exp, object resultVar)
        {
            var c = exp as Call;
            if (c == null)
                throw new SyntaxError("Invalid exclusion logic expression", exp);
            if (c.IsFunctor(Symbol.Slash, 1))
            {
                return new Call("write_nonexclusive", null, c.Arguments[0], resultVar);
            }
            if (c.IsFunctor(Symbol.Slash, 2))
            {
                if (c.Arguments[0] is Symbol)
                    return new Call("write_nonexclusive", c.Arguments[0], c.Arguments[1], resultVar);
                var temp = Variable.MakeGenerated("*Node*");
                var parentCode = ExpandWrite(c.Arguments[0], temp);
                return Macros.And(parentCode, new Call("write_nonexclusive", temp, c.Arguments[1], resultVar));
            }
            if (c.IsFunctor(Symbol.Colon, 2))
            {
                if (c.Arguments[0] is Symbol)
                    return new Call("write_exclusive", c.Arguments[0], c.Arguments[1], resultVar);
                var temp = Variable.MakeGenerated("*Node*");
                var parentCode = ExpandWrite(c.Arguments[0], temp);
                return Macros.And(parentCode, new Call("write_exclusive", temp, c.Arguments[1], resultVar));
            }
            if (c.IsFunctor(StoreNode, 2))
            {
                return ExpandWrite(c.Arguments[0], c.Arguments[1]);
            }
            throw new SyntaxError("Invalid exclusion logic expression", exp);
        }
        #endregion

        #region C# Mutators
        /// <summary>
        /// A do-nothing procedure used to make clear that a / % expression in C# is really intended to do a store.
        /// </summary>
        /// <param name="ignore">The ELNode that got stored.</param>
        // ReSharper disable once UnusedParameter.Global
        public static void Store(ELNode ignore)
        {
            // Does nothing
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public ELNode StoreExclusive(object newKey)
        {
            if (FirstChild == null)
            {
                IsExclusive = true;
                TaggedValue k = new TaggedValue();
                k.SetGeneral(newKey);
                FirstChild = Allocate(ref k, null, this);
                return FirstChild;
            }
            // We already have at least one child
            if (!IsExclusive)
                throw new InvalidOperationException("Exclusive store on non-exclusive ELNode: "+this);
            FirstChild.Key.SetGeneral(newKey);
            FirstChild.keyHash = FirstChild.Key.Hash();
            return FirstChild;
        }

        public ELNode StoreNonExclusive(object newKey)
        {
            if (IsExclusive)
                throw new InvalidOperationException("Non-exclusive store to an exclusive ELNode: "+this);
            TaggedValue k = new TaggedValue();
            k.SetGeneral(newKey);
            // If it already appears, in the children, just return it.
            var probe = FindChild(ref k);
            if (probe != null)
                return probe;
            // Otherwise make a new node.
            FirstChild = Allocate(ref k, FirstChild, this);
            if (FirstChild.NextSibling != null)
                FirstChild.NextSibling.previousSibling = FirstChild;
            return FirstChild;
        }

        /// <summary>
        /// Write the specified key as a non-exclusive child.  If key is already a child, this has no effect
        /// </summary>
        /// <param name="e">KB node</param>
        /// <param name="key">Key to write</param>
        /// <returns>The child node containing key.</returns>
        public static ELNode operator /(ELNode e, object key)
        {
            return e.StoreNonExclusive(key);
        }

        /// <summary>
        /// Write the specified key as an exclusive child.
        /// If key is already the child, this has no effect.
        /// Otherwise, the current child is replaced with this key.
        /// </summary>
        /// <param name="e">KB node</param>
        /// <param name="key">Key to write</param>
        /// <returns>The child node containing key.</returns>
        public static ELNode operator %(ELNode e, object key)
        {
            return e.StoreExclusive(key);
        }
        #endregion

        #region Printing
        /// <summary>
        /// Unparses the entry into key+key+key format
        /// </summary>
        /// <returns>Name in key+key+key format</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Unparses the entry into key+key+key format
        /// </summary>
        private string Name
        {
            get
            {
                if (parent == null)
                    return "/";
                var b = new StringBuilder();
                BuildName(b);
                return b.ToString();
            }
        }

        void BuildName(StringBuilder b)
        {
            if (parent?.parent != null)
            {
                parent.BuildName(b);
                b.Append(parent.IsExclusive? Symbol.Colon: Symbol.Slash);
            }
            b.Append(Key);
        }
        #endregion
    }
}
