﻿using System.Collections.Generic;

namespace Crayon.ParseTree
{
	public abstract class Node
	{
		internal Node(Token firstToken, Executable functionOrClassOwner)
		{
			this.FirstToken = firstToken;
			this.FunctionOrClassOwner = functionOrClassOwner;
		}

		public Token FirstToken { get; private set; }
		public Executable FunctionOrClassOwner { get; private set; }

		internal void BatchExecutableNameResolver(Parser parser, Dictionary<string, Executable> lookup, string[] imports, Executable[] executables)
		{
			for (int i = 0; i < executables.Length; ++i) 
			{
				executables[i] = executables[i].ResolveNames(parser, lookup, imports);
			}
		}

		internal void BatchExpressionNameResolver(Parser parser, Dictionary<string, Executable> lookup, string[] imports, Expression[] expressions)
		{
			for (int i = 0; i < expressions.Length; ++i)
			{
				if (expressions[i] != null)
				{
					expressions[i] = expressions[i].ResolveNames(parser, lookup, imports);
				}
			}
		}

		internal abstract void GetAllVariableNames(Dictionary<string, bool> lookup);

		internal abstract void SetLocalIdPass(VariableIdAllocator varIds);

		public static Executable DoNameLookup(Dictionary<string, Executable> lookup, string[] imports, string name)
		{
			if (lookup.ContainsKey(name))
			{
				return lookup[name];
			}

			foreach (string import in imports)
			{
				string path = import + "." + name;
				if (lookup.ContainsKey(path))
				{
					return lookup[path];
				}
			}

			return null;
		}
	}
}