﻿using System;
using System.Collections.Generic;

namespace Crayon.ParseTree
{
    internal class SystemFunctionCall : Expression
    {
        internal override Expression PastelResolve(Parser parser)
        {
            this.HACK_CoreLibraryReference = parser.SystemLibraryManager.GetLibraryFromKey("core");
            return this;
        }

        public override bool CanAssignTo { get { return false; } }
        public string Name { get; private set; }
        public Expression[] Args { get; private set; }
        public Library AssociatedLibrary { get; private set; }
        public Library HACK_CoreLibraryReference { get; set; }

        public SystemFunctionCall(Token token, Expression[] args, Executable owner)
            : base(token, owner)
        {
            this.Name = token.Value;
            this.Args = args;
        }

        internal override Expression Resolve(Parser parser)
        {
            for (int i = 0; i < this.Args.Length; ++i)
            {
                this.Args[i] = this.Args[i].Resolve(parser);
            }

            if (this.Name.StartsWith("$_lib_"))
            {
                string libraryName = this.Name.Split('_')[2];
                Library library = parser.SystemLibraryManager.GetLibraryFromKey(libraryName);
                if (library != null)
                {
                    this.AssociatedLibrary = library;
                }
            }

            // TODO: Eventaully remove this and have Core as a default when $_lib_ is not present.
            // This is part of an effort to move ALL system function calls to the Core library.
            this.HACK_CoreLibraryReference = parser.SystemLibraryManager.GetLibraryFromKey("core");
            
            // args have already been resolved.
            return this;
        }

        internal override Expression ResolveNames(Parser parser, System.Collections.Generic.Dictionary<string, Executable> lookup, string[] imports)
        {
            this.BatchExpressionNameResolver(parser, lookup, imports, this.Args);
            return this;
        }

        internal override void PerformLocalIdAllocation(VariableIdAllocator varIds, VariableIdAllocPhase phase)
        {
            if ((phase & VariableIdAllocPhase.ALLOC) != 0)
            {
                foreach (Expression arg in this.Args)
                {
                    arg.PerformLocalIdAllocation(varIds, phase);
                }
            }
        }

        internal override void GetAllVariablesReferenced(HashSet<Variable> vars)
        {
            foreach (Expression ex in this.Args)
            {
                ex.GetAllVariablesReferenced(vars);
            }
        }
    }
}
