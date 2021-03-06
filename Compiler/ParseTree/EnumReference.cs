﻿using System;
using System.Collections.Generic;

namespace Crayon.ParseTree
{
    internal class EnumReference : Expression
    {
        internal override Expression PastelResolve(Parser parser)
        {
            throw new NotImplementedException();
        }

        public EnumDefinition EnumDefinition { get; set; }

        public EnumReference(Token token, EnumDefinition enumDefinition, TopLevelConstruct owner)
            : base(token, owner)
        {
            this.EnumDefinition = enumDefinition;
        }

        public override bool CanAssignTo { get { return false; } }

        internal override Expression Resolve(Parser parser)
        {
            return this;
        }

        internal override Expression ResolveNames(Parser parser)
        {
            throw new NotImplementedException();
        }

        internal override void GetAllVariablesReferenced(HashSet<Variable> vars) { }
        internal override void PerformLocalIdAllocation(Parser parser, VariableIdAllocator varIds, VariableIdAllocPhase phase) { }
    }
}
