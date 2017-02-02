﻿using System;
using System.Collections.Generic;

namespace Pastel.Nodes
{
    class Assignment : Executable
    {
        public Expression Target { get; set; }
        public Token OpToken { get; set; }
        public Expression Value { get; set; }

        public Assignment(
            Expression target,
            Token opToken,
            Expression value) : base(target.FirstToken)
        {
            this.Target = target;
            this.OpToken = opToken;
            this.Value = value;
        }

        public override IList<Executable> ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            this.Target = this.Target.ResolveNamesAndCullUnusedCode(compiler);
            this.Value = this.Value.ResolveNamesAndCullUnusedCode(compiler);
            return Listify(this);
        }
    }
}
