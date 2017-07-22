#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CSVParser.cs" company="Ian Horswill">
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BotL.Compiler;

namespace BotL.Parser
{
    // ReSharper disable once InconsistentNaming
    class CSVParser
    {
        public CSVParser(char delimiter, PositionTrackingTextReader reader)
        {
            this.delimiter = delimiter;
            this.reader = reader;
        }

        /// <summary>
        /// Number of columns in the spreadsheet.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public int Arity { get; private set; }
        
        private readonly char delimiter;

        private readonly PositionTrackingTextReader reader;

        private readonly StringBuilder itemBuffer = new StringBuilder();
        
        private int rowNumber = 1;

        public readonly List<Symbol> Signature = new List<Symbol>();

        // ReSharper disable once InconsistentNaming
        public void Read(Action<int, object[]> rowHandler)
        {

            var row = 1;
            try
            {
                ReadHeaderRow();
                row++;
                while (reader.Peek() >= 0)
                {
                    // Windows excel generates invalid CSV files that contain
                    // \r\n rather than \r as is defined by the spec.
                    if (reader.Peek() == '\n')
                        reader.Read();
                    if (reader.Peek() == '%')
                        SkipLine(); // Skip comment lines
                    else
                        rowHandler(row, ReadFactRow());
                    row++;
                }
            }
            catch (Exception e)
            {
                var wrapper = new Exception($"{reader.File} row {row}: {e.Message}", e);
                throw wrapper;
            }
        }

        void SkipLine()
        {
            int c;
            do
            {
                c = reader.Read();
            }
            while (c != '\r');
        }

        void ReadHeaderRow()
        {
            Arity = 0;
            ReadRow(item =>
            {
                Signature.Add(DecodeColumnHeader(item));
                Arity++;
            });
        }

        private Symbol DecodeColumnHeader(string header)
        {
            var closeParen = header.LastIndexOf(')');
            if (closeParen < 0)
                return Symbol.Intern("string");
            var openParen = header.LastIndexOf('(', closeParen);
            if (openParen < 0)
                return Symbol.Intern("string");
            var type = header.Substring(openParen + 1, (closeParen - openParen)-1);
            switch (type)
            {
                case "int":
                    return Symbol.Intern("integer");

                default:
                    return Symbol.Intern(type);
            }
        }

        object[] ReadFactRow()
        {
            int argument = 0;
            List<object> row = new List<object>();
            ReadRow(
                item =>
                {
                    if (argument >= Arity)
                        if (argument != Arity)
                            throw new Exception("Too many columns in row " + rowNumber);
                    var cell = DecodeItemString(argument, item);
                    var columnType = Signature[argument];
                    switch (columnType.Name)
                    {
                        case "integer":
                        case "object":
                        case "float":
                        case "string":
                        case "symbol":
                        case "list":
                            row.Add(cell);
                            break;

                        default:
                            Structs.FlattenInto(cell, columnType, row);
                            break;
                    }
                    argument++;
                });
            if (argument != Arity)
                throw new Exception("Too few columns in row " + rowNumber);
            return row.ToArray();
        }

        private object DecodeItemString(int column, string item)
        {
            switch (Signature[column].Name)  // Columns are numbered from 1 :-(
            {
                case "float":
                    if (item == "")
                        return 0;
                    return float.Parse(item);

                case "integer":
                    if (item == "")
                        return 0;
                    return int.Parse(item);

                case "symbol":
                    if (item == "")
                        throw new SyntaxError($"Blank cell in column {column}, which should contain valid symbols.", item);
                    return Symbol.Intern(item);

                case "string":
                    return item;

                case "list":
                {
                    var items = item.Trim(' ').Split(',');
                    var result = new ArrayList();
                    foreach (var i in items)
                    {
                        var trimmed = i.Trim(' ');
                        if (trimmed != "")
                            result.Add(Symbol.Intern(trimmed));
                    }
                    return result.ToArray();
                }

                default:
                    return new ExpressionParser(item).Read();
            }
        }

        private void ReadRow(Action<string> itemHandler)
        {
            bool gotOne = false;
            int peek = reader.Peek();
            if (peek == delimiter)
            {
                // Edge case: line starts with a completely empty cell
                itemHandler("");
                gotOne = true;
            }

            while (peek >= 0)
            {
                if (peek == '\r' || peek == '\n')
                {
                    // end of line - swallow cr and/or lf
                    reader.Read();
                    if (peek == '\r')
                    {
                        // Swallow LF if CRLF
                        peek = reader.Peek();
                        if (peek == '\n')
                            reader.Read();
                    }
                    rowNumber++;
                    return;
                }
                if (peek == delimiter)
                    // Skip over delimiter
                    reader.Read();

                var item = ReadItem(reader, delimiter, itemBuffer);
                if (!gotOne && item.StartsWith("//"))
                {
                    int c;
                    // This is a comment line; skip it
                    do
                    {
                        c = reader.Read();
                    } while (c != '\r' && c != '\n');
                    peek = reader.Peek();
                    while (peek == '\r' || peek == '\n')
                    {
                        reader.Read();
                        peek = reader.Peek();
                    }
                    continue;
                }
                gotOne = true;
                itemHandler(item);
                peek = reader.Peek();
            }
        }

        static string ReadItem(TextReader reader, char delimiter, StringBuilder stringBuilder)
        {
            bool quoted = false;
            stringBuilder.Length = 0;
            int peek = (char)reader.Peek();
            if (peek == delimiter)
                return "";
            if (peek == '\"')
            {
                quoted = true;
                reader.Read();
            }
            getNextChar:
            peek = reader.Peek();
            if (peek < 0)
                goto done;
            if (quoted && peek == '\"')
            {
                reader.Read();  // Swallow quote
                if ((char)reader.Peek() == '\"')
                {
                    // It was an escaped quote
                    reader.Read();
                    stringBuilder.Append('\"');
                    goto getNextChar;
                }
                // It was the end of the item
                // ReSharper disable RedundantJumpStatement
                goto done;
                // ReSharper restore RedundantJumpStatement
            }
            if (!quoted && (peek == delimiter || peek == '\r' || peek == '\n'))
                // ReSharper disable RedundantJumpStatement
                goto done;
            // ReSharper restore RedundantJumpStatement
            stringBuilder.Append((char)peek);
            reader.Read();
            goto getNextChar;
            //System.Diagnostics.Debug.Assert(false, "Line should not be reachable.");
            done:
            return stringBuilder.ToString();
        }
    }
}
