#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ELNode.cs" company="Ian Horswill">
// Copyright (C) 2017 Ian Horswill
//  
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//  
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using BotL.Compiler;
using JetBrains.Annotations;
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
#if DEBUG
            Debug.Assert(!node.inUse, "Allocating ELNode that's already in use");
#endif
            node.Key = key;
            node.keyHash = key.Hash();
            node.FirstChild = null;
            node.previousSibling = null;
            node.NextSibling = nextSibling;
            node.Parent = parent;
            node.IsExclusive = false;
#if DEBUG
            node.inUse = true;
#endif
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
#if DEBUG
            Debug.Assert(inUse, "Freeing free ELNode");
#endif
            var c = FirstChild;
            while (c != null)
            {
                c.Deallocate();
                c = c.NextSibling;
            }
#if DEBUG
            inUse = false;
#endif
            NodePool.Push(this);
        }

        private ELNode(ELNode parent)
        {
            Parent = parent;
        }

        private ELNode(ref TaggedValue key, ELNode nextSibling, ELNode parent)
        {
            Key = key;
            keyHash = key.Hash();
            NextSibling = nextSibling;
            Parent = parent;
        }
        #endregion

        #region Instance fields
#if DEBUG
        private bool inUse = true;  // Sanity checking for allocation/deallocation problems.
#endif
        internal bool IsExclusive;
        public ELNode Parent { get; private set; }
        // ReSharper disable once MemberCanBePrivate.Global
        public TaggedValue Key;
        private int keyHash;

        internal ELNode NextSibling;
        private ELNode previousSibling;
        internal ELNode FirstChild;
        #endregion

        #region Properties
        public int ChildIntValue
        {
            get
            {
                if (FirstChild != null)
                {
                    if (!IsExclusive)
                        throw new InvalidOperationException("ChildIntValue called on non-exclusive node: " + this);
                    if (FirstChild.Key.Type != TaggedValueType.Integer)
                        throw new InvalidOperationException(
                            "ChildIntValue called on node whose child key is the wrong type: " + FirstChild.Key.Value);
                    return FirstChild.Key.integer;
                }
                throw new InvalidOperationException("ChildIntValue called on node with no children: " + this);
            }
        }

        public float ChildFloatValue
        {
            get
            {
                if (FirstChild != null)
                {
                    if (!IsExclusive)
                        throw new InvalidOperationException("ChildFloatValue called on non-exclusive node: " + this);
                    if (FirstChild.Key.Type != TaggedValueType.Float && FirstChild.Key.Type != TaggedValueType.Integer)
                        throw new InvalidOperationException(
                            "ChildFloatValue called on node whose child key is the wrong type: " + FirstChild.Key.Value);
                    return FirstChild.Key.AsFloat;
                }
                throw new InvalidOperationException("ChildIntValue called on node with no children: " + this);
            }
        }

        public bool ChildBoolValue
        {
            get
            {
                if (FirstChild != null)
                {
                    if (!IsExclusive)
                        throw new InvalidOperationException("ChildBoolValue called on non-exclusive node: " + this);
                    if (FirstChild.Key.Type != TaggedValueType.Boolean)
                        throw new InvalidOperationException(
                            "ChildBoolValue called on node whose child key is the wrong type: " + FirstChild.Key.Value);
                    return FirstChild.Key.boolean;
                }
                throw new InvalidOperationException("ChildBoolValue called on node with no children: " + this);
            }
        }

        public object ChildValue
        {
            get
            {
                if (FirstChild != null)
                {
                    if (!IsExclusive)
                        throw new InvalidOperationException("ChildValue called on non-exclusive node: " + this);
                    
                    return FirstChild.Key.Value;
                }
                throw new InvalidOperationException("ChildValue called on node with no children: " + this);
            }
        }
        #endregion

        #region Iterators
        [UsedImplicitly]
        public IEnumerable<int> AllChildIntValues
        {
            get
            {
                for (var c = FirstChild; c != null; c = c.NextSibling)
                {
                    if (c.Key.Type != TaggedValueType.Integer)
                        throw new InvalidOperationException(
                            "ChildIntValues called on node with child wrong key type: " + c.Key.Value);
                    yield return c.Key.integer;
                }
            }
        }

        [UsedImplicitly]
        public IEnumerable<float> AllChildFloatValues
        {
            get
            {
                for (var c = FirstChild; c != null; c = c.NextSibling)
                {
                    if (c.Key.Type != TaggedValueType.Float)
                        throw new InvalidOperationException(
                            "ChildIntValues called on node with child wrong key type: " + c.Key.Value);
                    yield return c.Key.floatingPoint;
                }
            }
        }

        [UsedImplicitly]
        public IEnumerable<object> AllChildValues
        {
            get
            {
                for (var c = FirstChild; c != null; c = c.NextSibling)
                {
                    yield return c.Key.Value;
                }
            }
        }

        [UsedImplicitly]
        public IEnumerable<T> EnumerateChildValues<T>() where T: class
        {
            for (var c = FirstChild; c != null; c = c.NextSibling)
            {
                T value = c.Key.Value as T;
                if (value == null)
                    throw new InvalidOperationException(
                            $"EnumerateChildValues<{typeof(T).Name}> called on node with child wrong key type: {c.Key.Value}");
                yield return value;
            }
        }

        [UsedImplicitly]
        public int ChildCount
        {
            get
            {
                int count = 0;
                for (var c = FirstChild; c != null; c = c.NextSibling)
                    count++;
                return count;
            }
        }
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
                Parent.FirstChild = NextSibling;
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
            KB.DefinePrimop("read_exclusive", 3, (argBase, restartCount) => ReadEL(argBase, restartCount, true),
                semiDeterministic: true);
            KB.DefinePrimop("write_nonexclusive!", 3, (argBase, restartCount) => WriteEL(argBase, false),
                deterministic: true);
            KB.DefinePrimop("write_nonexclusive_to_end!", 3, (argBase, restartCount) => WriteEL(argBase, false, true),
                deterministic: true);
            KB.DefinePrimop("write_exclusive!", 3, (argBase, restartCount) => WriteEL(argBase, true),
                deterministic: true);
            KB.DefinePrimop("delete_el_node!", 1, (argBase, ignore) =>
            {
                ((ELNode)DataStack[Deref(argBase)].reference).Delete();
                return CallStatus.DeterministicSuccess;
            },
            deterministic: true);
        }

        private static ELNode DecodeNodeArg(ushort stackAddress)
        {
            var deref = Deref(stackAddress);
            var nodeArg = DataStack[deref].reference;
            if (nodeArg == null)
                return Root;

            var node = nodeArg as ELNode;
            if (DataStack[deref].Type != TaggedValueType.Reference || !(nodeArg is ELNode))
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
            Debug.Assert(!isExclusive || restartCount == 0);
            var parentNode = DecodeNodeArg(argBase);
            if (parentNode.IsExclusive != isExclusive && parentNode.FirstChild != null)
            {
                if (isExclusive)
                    throw new ArgumentException("Exclusive read of non-exclusive EL node "+parentNode);
                throw new ArgumentException("Non-exclusive read of exclusive EL node " + parentNode);
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
                    child = parentNode.FirstChild;

                    if (child == null)
                        return CallStatus.Fail;
                }
                else
                {
                    // TODO - FIX THIS!  We're doing a linear lookup because the output argument
                    // isn't actually holding the previous solution.  I'm not going to worry about it
                    // for now since (1) it's good enough for the class and (2) we need to implement
                    // hash table lookup later anyhow.
                    child = parentNode.FirstChild;
                    for (int i = 0; i < restartCount; i++)
                        child = child.NextSibling;
                    // We know DataStack[resultAddr].reference still has the previous result.
                    //var previousResult = ((ELNode)DataStack[resultAddr].reference);
                    //Debug.Assert(previousResult != null, "Previous result is null");
                    //child = previousResult.NextSibling;
                    //Debug.Assert(child != null);
                    //Trace.WriteLine($"Parent={parentNode}, previous={previousResult}, child={child}");
                }
                
                Debug.Assert(!parentNode.IsExclusive || child.NextSibling == null, "Exclusive node has multiple children");
                DataStack[keyAddr] = child.Key;
                DataStack[resultAddr].SetReference(child);
                SaveVariable(keyAddr);
                SaveVariable(resultAddr);
                if (child.NextSibling == null)
                    return CallStatus.DeterministicSuccess;
                else
                {
                    Debug.Assert(!isExclusive);
                    return CallStatus.NonDeterministicSuccess;
                }
            }

            // Second arg is bound, so we're matching to it
            var match = parentNode.FindChild(ref DataStack[keyAddr]);
            if (match == null)
                return CallStatus.Fail;
            DataStack[resultAddr].SetReference(match);
            SaveVariable(resultAddr);
            return CallStatus.DeterministicSuccess;
        }

        private static CallStatus WriteEL(ushort argBase, bool isExclusive, bool atEnd=false)
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
                    if (atEnd)
                    {
                        if (node.FirstChild == null)
                            result = node.FirstChild = Allocate(ref DataStack[keyAddr], node.FirstChild, node);
                        else
                        {
                            var last = node.FirstChild;
                            while (last.NextSibling != null) last = last.NextSibling;
                            result = last.NextSibling = Allocate(ref DataStack[keyAddr], null, node);
                        }
                    }
                    else
                    {
                        result = node.FirstChild = Allocate(ref DataStack[keyAddr], node.FirstChild, node);
                        if (result.NextSibling != null)
                            result.NextSibling.previousSibling = result;
                    }
                }
            }
            DataStack[resultAddr].SetReference(result);
            SaveVariable(resultAddr);
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
                if (c.Arguments[0] is Symbol || Call.IsFunctor(c.Arguments[0], Symbol.DollarSign, 1))
                    return new Call("read_nonexclusive", c.Arguments[0], c.Arguments[1], resultVar);
                var temp = Variable.MakeGenerated("*Node*");
                var parentCode = ExpandEL(c.Arguments[0], temp);
                return Macros.And(parentCode, new Call("read_nonexclusive", temp, c.Arguments[1], resultVar));
            }
            if (c.IsFunctor(Symbol.Colon, 2))
            {
                if (c.Arguments[0] is Symbol || Call.IsFunctor(c.Arguments[0], Symbol.DollarSign, 1))
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
            if (updateOperation.Name == "assert_internal!")
                return ExpandWrite(elExpr, Symbol.Underscore);
            if (updateOperation.Name == "retract_internal!")
            {
                if (Variable.IsVariableExpression(elExpr))
                    return new Call("delete_el_node!", elExpr);

                var node = Variable.MakeGenerated("*Node*");
                var lookup = ExpandEL(elExpr, node);
                return Macros.And(lookup, new Call("delete_el_node!", node));
            }
            throw new InvalidOperationException($"Cannot perform {updateOperation} on EL database");
        }

        internal static readonly Symbol WriteToEnd = Symbol.Intern("/>");
        private static object ExpandWrite(object exp, object resultVar)
        {
            var c = exp as Call;
            if (c == null)
                throw new SyntaxError("Invalid exclusion logic expression", exp);
            if (c.IsFunctor(Symbol.Slash, 1))
            {
                return new Call("write_nonexclusive!", null, c.Arguments[0], resultVar);
            }
            if (c.IsFunctor(Symbol.Slash, 2))
            {
                if (c.Arguments[0] is Symbol || Call.IsFunctor(c.Arguments[0], Symbol.DollarSign, 1))
                    return new Call("write_nonexclusive!", c.Arguments[0], c.Arguments[1], resultVar);
                var temp = Variable.MakeGenerated("*Node*");
                var parentCode = ExpandWrite(c.Arguments[0], temp);
                return Macros.And(parentCode, new Call("write_nonexclusive!", temp, c.Arguments[1], resultVar));
            }
            if (c.IsFunctor(WriteToEnd, 2))
            {
                if (c.Arguments[0] is Symbol || Call.IsFunctor(c.Arguments[0], Symbol.DollarSign, 1))
                    return new Call("write_nonexclusive_to_end!", c.Arguments[0], c.Arguments[1], resultVar);
                var temp = Variable.MakeGenerated("*Node*");
                var parentCode = ExpandWrite(c.Arguments[0], temp);
                return Macros.And(parentCode, new Call("write_nonexclusive_to_end!", temp, c.Arguments[1], resultVar));
            }
            if (c.IsFunctor(Symbol.Colon, 2))
            {
                if (c.Arguments[0] is Symbol || Call.IsFunctor(c.Arguments[0], Symbol.DollarSign, 1))
                    return new Call("write_exclusive!", c.Arguments[0], c.Arguments[1], resultVar);
                var temp = Variable.MakeGenerated("*Node*");
                var parentCode = ExpandWrite(c.Arguments[0], temp);
                return Macros.And(parentCode, new Call("write_exclusive!", temp, c.Arguments[1], resultVar));
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

        public ELNode StoreNonExclusive(object newKey, bool atEnd = false)
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
            if (atEnd)
            {
                if (FirstChild == null)
                    return FirstChild = Allocate(ref k, FirstChild, this);
                else
                {
                    var last = FirstChild;
                    while (last.NextSibling != null) last = last.NextSibling;
                    last.NextSibling = Allocate(ref k, null, this);
                    return last.NextSibling;
                }
            }
            else
            {
                FirstChild = Allocate(ref k, FirstChild, this);
                if (FirstChild.NextSibling != null)
                    FirstChild.NextSibling.previousSibling = FirstChild;
                return FirstChild;
            }
        }

        [UsedImplicitly]
        public void DeleteAllChildren()
        {
            while (FirstChild != null)
                FirstChild.Delete();
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
                if (Parent == null)
                    return "@(/)";
                var b = new StringBuilder();
                b.Append("@(");
                BuildName(b);
                b.Append(")");
                return b.ToString();
            }
        }

        void BuildName(StringBuilder b)
        {
            if (Parent == null)
                // We're the root
                b.Append("/");
            else
            {
                Parent.BuildName(b);
                if (Parent != Root)
                    b.Append(Parent.IsExclusive? Symbol.Colon: Symbol.Slash);
                b.Append(Key);
            }
        }
        #endregion
    }
}
