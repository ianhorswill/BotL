#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BindingEnvironment.cs" company="Ian Horswill">
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
using System.Collections.Generic;

namespace BotL.Compiler
{
    class BindingEnvironment
    {
        private readonly Dictionary<Symbol, Variable> variableTable = new Dictionary<Symbol, Variable>();
        private readonly Dictionary<Variable, VariableInfo> variableInfoTable = new Dictionary<Variable, VariableInfo>();
        private ushort nextIndex;


        public void IncrementVoidVariableReferences()
        {
            foreach (var b in variableInfoTable)
            {
                if (b.Key.Name.Name != "_" && b.Value.Uses == 1)
                    b.Value.BodyUses += 1;
            }
        }

        public IEnumerable<KeyValuePair<Symbol, object>> FrameBindings(ushort baseAddress)
        {
            foreach (var b in variableInfoTable)
            {
                var frameIndex = b.Value.EnvironmentIndex;
                if (frameIndex >= 0 && !b.Key.IsGenerated)
                {
                    var address = Engine.Deref(baseAddress+frameIndex);
                    var value = Engine.DataStack[address];
                    if (value.Type == TaggedValueType.Unbound)
                        // TODO: Fix this to return a real value, not a bogus string.
                        yield return new KeyValuePair<Symbol, object>(b.Key.Name, new UnboundVariableStackReference(address));
                    else
                        yield return new KeyValuePair<Symbol, object>(b.Key.Name, value.Value);
                }
            }
        }

        /// <summary>
        /// A placeholder object that prints as "_address".
        /// Used to communicate top level variable values back to the repl.
        /// </summary>
        private struct UnboundVariableStackReference
        {
            private readonly ushort address;

            public UnboundVariableStackReference(ushort address)
            {
                this.address = address;
            }

            public override string ToString()
            {
                return $"_{address}";
            }
        }
 
        public Variable this[Symbol s]
        {
            get
            {
                Variable v;
                if (s.Name == "_" || !variableTable.TryGetValue(s, out v))
                {
                    v = new Variable(s);
                    variableTable[s] = v;
                    variableInfoTable[v] = new VariableInfo();
                }
                return v;
            }
        }

        public ushort EnvironmentSize => nextIndex;

        public VariableInfo this[Variable v]
        {
            get
            {
                VariableInfo i;
                if (variableInfoTable.TryGetValue(v, out i))
                    return i;
                variableInfoTable[v] = i = new VariableInfo();
                return i;
            }
        }

        public void AllocateSlot(VariableInfo variableInfo)
        {
            variableInfo.EnvironmentIndex = nextIndex++;
        }
    }
}
