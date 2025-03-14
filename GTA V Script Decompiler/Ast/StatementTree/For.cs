﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decompiler.Ast.StatementTree
{
    internal class For : Tree
    {
        public readonly AstToken Initializer;
        public readonly AstToken Condition;
        public readonly AstToken Increment; // well it doesn't have to be an increment but couldn't find a better name for this
        public For(Function function, Tree parent, int offset, AstToken initializer, AstToken condition, AstToken increment) : base(function, parent, offset)
        {
            Initializer = initializer;
            Condition = condition;
            Increment = increment;
        }

        /// <returns>Isn't going to be called anyway</returns>
        public override bool IsTreeEnd()
        {
            return true;
        }

        public override string ToString()
        {
            string increment = Increment.ToString();
            increment = increment.Substring(0, increment.Length - 1); // remove trailing semicolon, ugly hack
            return $"for ({Initializer} {Condition}; {increment}){Environment.NewLine}{{{Environment.NewLine}{base.ToString()}}}";
        }
    }
}
