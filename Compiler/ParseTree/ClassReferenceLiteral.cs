﻿using System;
using System.Collections.Generic;

namespace Crayon.ParseTree
{
    internal class ClassReferenceLiteral : Expression
    {
        internal override Expression PastelResolve(Parser parser)
        {
            throw new NotImplementedException();
        }

        public ClassDefinition ClassDefinition { get; set; }

        public ClassReferenceLiteral(Token firstToken, ClassDefinition cd, TopLevelConstruct owner)
            : base(firstToken, owner)
        {
            this.ClassDefinition = cd;
        }

        public override bool CanAssignTo { get { return false; } }

        internal override void GetAllVariablesReferenced(HashSet<Variable> vars) { }

        internal override void PerformLocalIdAllocation(Parser parser, VariableIdAllocator varIds, VariableIdAllocPhase phase) { }

        internal override Expression Resolve(Parser parser) { return this; }

        internal override Expression ResolveNames(Parser parser)
        {
            // ClassReferenceLiteral is created in the Resolve pass, so this is never called.
            throw new InvalidOperationException();
        }
    }
}
