﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TaggedValue.cs" company="Ian Horswill">
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
using System.Globalization;
using System.Runtime.InteropServices;
using BotL.Parser;

namespace BotL
{
    /// <summary>
    /// A variant record used to represent values in the interpreter.
    /// We use this instead of the object type because if you store an int, bool, or float into an object
    /// variable, the system will box it (copy it to the heap).  So we instead have a struct (non-heap) type 
    /// that is optimized to be able to hold an int, bool, or float without boxing.
    /// This is the type used to represent variables on the stack.  It can also store forwarding pointers
    /// to other variables when this variable has been unified with another, or a special "unbound" value.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct TaggedValue
    {
        // Type tag
        [FieldOffset(0)]
        public TaggedValueType Type;
        // Tagged value types
        [FieldOffset(4)] public int integer;
        [FieldOffset(4)] public bool boolean;
        [FieldOffset(4)] public float floatingPoint;
        // Forwarding pointer to another variable
        [FieldOffset(4)] public ushort forward;
        // This is the catch-all for everything that isn't one of the above
        [FieldOffset(8)] public object reference;
        
        /// <summary>
        /// Convert value to an object reference, boxing it if necessary.
        /// </summary>
        public object Value
        {
            get
            {
                switch (Type)
                {
                    case TaggedValueType.Boolean:
                        return boolean;
                    case TaggedValueType.Float:
                        return floatingPoint;
                    case TaggedValueType.Integer:
                        return integer;
                    case TaggedValueType.Reference:
                        return reference;
                    default:
                        throw new InvalidOperationException("Attempt to get value with typecode " + Type);
                }
            }
        }

        /// <summary>
        /// Coerce it to a float, if possible, else throw an exception
        /// </summary>
        public float AsFloat
        {
            get
            {
                switch (Type)
                {
                    case TaggedValueType.Integer:
                        return integer;

                        case TaggedValueType.Float:
                        return floatingPoint;

                    default:
                        throw new InvalidOperationException("Attempt to convert a non-number to a float: "+ValueOrUnbound);
                }
            }
        }

        public object ValueOrUnbound
        {
            get
            {
                switch (Type)
                {
                    case TaggedValueType.Unbound:
                        return "<Unbound>";

                    case TaggedValueType.VariableForward:
                        return Engine.DataStack[integer].ValueOrUnbound;

                    default:
                        return Value;
                }
            }
        }

        #region Assignment
        public void Set(bool b)
        {
            Type = TaggedValueType.Boolean;
            boolean = b;
        }

        public void Set(int i)
        {
            Type = TaggedValueType.Integer;
            integer = i;
        }

        public void Set(float f)
        {
            Type = TaggedValueType.Float;
            floatingPoint = f;
        }

        public void SetReference(object o)
        {
            Type = TaggedValueType.Reference;
            reference = o;
        }

        public void SetGeneral(object value)
        {
            if (value is bool)
                Set((bool)value);
            else if (value is int)
                Set((int)value);
            else if (value is float || value is double)
                Set(Convert.ToSingle(value));
            else
                SetReference(value);
        }
        #endregion

        #region Equality testing
        public bool Equal(bool b)
        {
            return Type == TaggedValueType.Boolean && boolean == b;
        }

        public bool Equal(int i)
        {
            return Type == TaggedValueType.Integer && integer == i;
        }

        public bool Equal(float f)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return Type == TaggedValueType.Float && floatingPoint == f;
        }

        public bool EqualReference(object o)
        {
            return Type == TaggedValueType.Reference && Equals(reference, o);
        }

        public bool EqualGeneral(object value)
        {
            switch (Type)
            {
                case TaggedValueType.Boolean:
                    return value is bool && (bool) value == boolean;

                case TaggedValueType.Integer:
                    return value is int && (int)value == integer;

                case TaggedValueType.Float:
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    return value is float && (float)value == floatingPoint;

                case TaggedValueType.Reference:
                    return Equals(reference, value);

                default:
                    throw new InvalidOperationException("Invalid tag type: "+Type);
            }
        } 
        #endregion

        #region Unification support
        /// <summary>
        /// Alias this variable to another variable at the specified address in Engine.DataStack[]
        /// </summary>
        /// <param name="address">Address of variable to alias to.</param>
        public void AliasTo(ushort address)
        {
            Type = TaggedValueType.VariableForward;
            forward = address;
        }

        public void AliasTo(int address)
        {
            AliasTo((ushort)address);
        }
        #endregion

        public override string ToString()
        {
            switch (Type)
            {
                case TaggedValueType.Boolean:
                    return boolean.ToString();

                case TaggedValueType.Float:
                    return floatingPoint.ToString(CultureInfo.InvariantCulture);

                case TaggedValueType.Integer:
                    return integer.ToString();

                case TaggedValueType.Reference:
                    return reference == null ? "null" : ExpressionParser.WriteExpressionToString(reference);

                case TaggedValueType.Unbound:
                    return "Unbound";

                case TaggedValueType.VariableForward:
                    return $"(Forward to {forward}";

                default:
                    throw new InvalidOperationException("Invalid tag type: " + Type);
            }
        }

        /// <summary>
        /// Test of tagged value matches this value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Match(ref TaggedValue value)
        {
            if (Type != value.Type)
                return false;
            switch (Type)
            {
                case TaggedValueType.Boolean:
                    return value.boolean == boolean;

                case TaggedValueType.Float:
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    return value.floatingPoint == floatingPoint;

                case TaggedValueType.Integer:
                    return value.integer == integer;

                case TaggedValueType.Reference:
                    return Equals(value.reference, reference);

                default:
                    throw new InvalidOperationException("Attempt to match non-constant TaggedValues");
            }
        }

        /// <summary>
        /// This maybe ought to be an overload of GetHashCode, but I'm not certain its semantics
        /// match those of GetHashCode, so I'm naming it Hash.
        /// </summary>
        internal int Hash()
        {
            switch (Type)
            {
                case TaggedValueType.Boolean:
                    return boolean.GetHashCode();

                case TaggedValueType.Float:
                    return floatingPoint.GetHashCode();

                case TaggedValueType.Integer:
                    return integer.GetHashCode();

                case TaggedValueType.Reference:
                    return reference?.GetHashCode() ?? 0xdeadbe1;

                default:
                    throw new InvalidOperationException("Attempt to hash non-constant TaggedValues");
            }
        }
    }
}
