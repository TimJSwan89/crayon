﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Crayon.ParseTree
{
	internal class ClassDefinition : Executable
	{
		private static int classIdAlloc = 1;

		public int ClassID { get; private set; }
		public ClassDefinition BaseClass { get; private set; }
		public Token NameToken { get; private set; }
		public Token[] BaseClassTokens { get; private set; }
		public string[] BaseClassDeclarations { get; private set; }
		// TODO: interface definitions.
		public FunctionDefinition[] Methods { get; set; }
		public ConstructorDefinition Constructor { get; set; }
		public ConstructorDefinition StaticConstructor { get; set; }
		public FieldDeclaration[] Fields { get; set; }
		public string Namespace { get; set; }

		private bool memberIdsResolved = false;

		// When a variable in this class is not locally defined, look for a fully qualified name that has one of these prefixes.

		private Dictionary<string, FunctionDefinition> functionDefinitionsByName = null;
		private Dictionary<string, FieldDeclaration> fieldDeclarationsByName = null;

		public ClassDefinition(
			Token classToken, 
			Token nameToken, 
			IList<Token> subclassTokens,
			IList<string> subclassNames,
			string ns, 
			Executable owner)
			: base(classToken, owner)
		{
			this.ClassID = ClassDefinition.classIdAlloc++;

			this.Namespace = ns;
			this.NameToken = nameToken;
			this.BaseClassTokens = subclassTokens.ToArray();
			this.BaseClassDeclarations = subclassNames.ToArray();
		}

		internal override void GenerateGlobalNameIdManifest(VariableIdAllocator varIds)
		{
			varIds.RegisterVariable(this.NameToken.Value);
			foreach (FieldDeclaration fd in this.Fields)
			{
				fd.GenerateGlobalNameIdManifest(varIds);
			}
			if (this.StaticConstructor != null)
			{
				this.StaticConstructor.GenerateGlobalNameIdManifest(varIds);
			}
			if (this.Constructor != null)
			{
				this.Constructor.GenerateGlobalNameIdManifest(varIds);
			}
			foreach (FunctionDefinition fd in this.Methods)
			{
				fd.GenerateGlobalNameIdManifest(varIds);
			}
		}

		public void AllocateLocalScopeIds()
		{
			foreach (FieldDeclaration fd in this.Fields)
			{
				fd.VerifyNoVariablesAreDereferenced();
			}

			// null check has occurred before now.
			this.Constructor.AllocateLocalScopeIds();

			if (this.StaticConstructor != null)
			{
				this.StaticConstructor.AllocateLocalScopeIds();
			}

			foreach (FunctionDefinition fd in this.Methods)
			{
				fd.AllocateLocalScopeIds();
			}
		}

		public FunctionDefinition GetMethod(string name, bool walkUpBaseClasses)
		{
			if (this.functionDefinitionsByName == null)
			{
				this.functionDefinitionsByName = new Dictionary<string, FunctionDefinition>();

				foreach (FunctionDefinition fd in this.Methods)
				{
					this.functionDefinitionsByName[fd.NameToken.Value] = fd;
				}
			}

			if (this.functionDefinitionsByName.ContainsKey(name))
			{
				return this.functionDefinitionsByName[name];
			}

			if (walkUpBaseClasses && this.BaseClass != null)
			{
				return this.BaseClass.GetMethod(name, walkUpBaseClasses);
			}

			return null;
		}

		public FieldDeclaration GetField(string name, bool walkUpBaseClasses)
		{
			if (this.fieldDeclarationsByName == null)
			{
				this.fieldDeclarationsByName = new Dictionary<string, FieldDeclaration>();

				int staticMemberId = 0;

				foreach (FieldDeclaration fd in this.Fields)
				{
					this.fieldDeclarationsByName[fd.NameToken.Value] = fd;
					if (fd.IsStaticField)
					{
						fd.StaticMemberID = staticMemberId++;
					}
				}
			}

			if (this.fieldDeclarationsByName.ContainsKey(name))
			{
				return this.fieldDeclarationsByName[name];
			}

			if (walkUpBaseClasses && this.BaseClass != null)
			{
				return this.BaseClass.GetField(name, walkUpBaseClasses);
			}

			return null;
		}

		internal override IList<Executable> Resolve(Parser parser)
		{
			if (parser.IsInClass)
			{
				throw new ParserException(this.FirstToken, "Nested classes aren't a thing, yet.");
			}

			if (Parser.IsReservedKeyword(this.NameToken.Value))
			{
				throw new ParserException(this.NameToken, "'" + this.NameToken.Value + "' is a reserved keyword.");
			}

			parser.CurrentClass = this;

			for (int i = 0; i < this.Fields.Length; ++i)
			{
				this.Fields[i] = (FieldDeclaration)this.Fields[i].Resolve(parser)[0];
			}

			for (int i = 0; i < this.Methods.Length; ++i)
			{
				this.Methods[i] = (FunctionDefinition)this.Methods[i].Resolve(parser)[0];
			}

			this.Constructor.Resolve(parser);
			if (this.StaticConstructor != null)
			{
				this.StaticConstructor.Resolve(parser);
			}

			parser.CurrentClass = null;

			bool hasABaseClass = this.BaseClass != null;
			bool callsBaseConstructor = this.Constructor.BaseToken != null;
			if (hasABaseClass && callsBaseConstructor)
			{
				Expression[] defaultValues = this.BaseClass.Constructor.DefaultValues;
				int maxValues = defaultValues.Length;
				int minValues = 0;
				for (int i = 0; i < maxValues; ++i)
				{
					if (defaultValues[i] == null) minValues++;
					else break;
				}
				int baseArgs = this.Constructor.BaseArgs.Length;
				if (baseArgs < minValues || baseArgs > maxValues)
				{
					throw new ParserException(this.Constructor.BaseToken, "Invalid number of arguments passed to base constructor.");
				}
			}
			else if (hasABaseClass && !callsBaseConstructor)
			{
				if (this.BaseClass.Constructor != null)
				{
					throw new ParserException(this.FirstToken, "The base class of this class has a constructor which must be called.");
				}
			}
			else if (!hasABaseClass && callsBaseConstructor)
			{
				throw new ParserException(this.Constructor.BaseToken, "Cannot call base constructor without a base class.");
			}
			else // (!hasABaseClass && !callsBaseConstructor)
			{
				// yeah, that's fine.
			}

			return Listify(this);
		}

		internal override void CalculateLocalIdPass(VariableIdAllocator varIds)
		{
			throw new System.InvalidOperationException(); // never call this directly on a class.
		}

		internal override void SetLocalIdPass(VariableIdAllocator varIds)
		{
			throw new System.InvalidOperationException(); // never call this directly on a class.
		}

		public void ResolveBaseClasses(Dictionary<string, Executable> lookup, string[] imports)
		{
			List<ClassDefinition> baseClasses = new List<ClassDefinition>();
			List<Token> baseClassesTokens = new List<Token>();
			for (int i = 0; i < this.BaseClassDeclarations.Length; ++i)
			{
				string value = this.BaseClassDeclarations[i];
				Token token = this.BaseClassTokens[i];
				Executable baseClassInstance = Executable.DoNameLookup(lookup, imports, value);
				if (baseClassInstance == null)
				{
					throw new ParserException(token, "Class not found.");
				}

				if (baseClassInstance is ClassDefinition)
				{
					baseClasses.Add((ClassDefinition)baseClassInstance);
					baseClassesTokens.Add(token);
				}
				// TODO: else if (baseClassInstance is InterfaceDefinition) { ... }
				else
				{
					throw new ParserException(token, "This is not a class.");
				}
			}

			if (baseClasses.Count > 1)
			{
				throw new ParserException(baseClassesTokens[1], "Multiple base classes found. Did you mean to use an interface?");
			}

			if (baseClasses.Count == 1)
			{
				this.BaseClass = baseClasses[0];
			}
		}

		private string[] ExpandImportsToIncludeThis(string[] imports)
		{
			// Tack on this class as an import. Once classes/enums/constants can be nested inside other classes, it'll be important.
			string thisClassFullyQualified = this.Namespace;
			if (thisClassFullyQualified.Length > 0) thisClassFullyQualified += ".";
			thisClassFullyQualified += this.NameToken.Value;
			List<string> newImports = new List<string>(imports);
			newImports.Add(thisClassFullyQualified);
			return newImports.ToArray();
		}

		internal override Executable ResolveNames(Parser parser, Dictionary<string, Executable> lookup, string[] imports)
		{
			imports = this.ExpandImportsToIncludeThis(imports);

			foreach (FieldDeclaration fd in this.Fields)
			{
				fd.ResolveNames(parser, lookup, imports);
			}

			if (this.StaticConstructor != null)
			{
				this.StaticConstructor.ResolveNames(parser, lookup, imports);
			}

			// TODO: if there is no constructor, just create an implicit one.
			// This should be empty if there is no base class, or just pass along the base class' args if there is.
			if (this.Constructor == null)
			{
				this.Constructor = new ConstructorDefinition(this.FirstToken, new Token[0], new Expression[0], new Expression[0], new Executable[0], null, this);
			}

			this.Constructor.ResolveNames(parser, lookup, imports);
			this.BatchExecutableNameResolver(parser, lookup, imports, this.Methods);

			return this;
		}

		public void VerifyNoBaseClassLoops()
		{
			if (this.BaseClass == null) return;

			HashSet<int> classIds = new HashSet<int>();
			ClassDefinition walker = this;
			while (walker != null)
			{
				if (classIds.Contains(walker.ClassID))
				{
					throw new ParserException(this.FirstToken, "This class' parent class hierarchy creates a cycle.");
				}
				classIds.Add(walker.ClassID);
				walker = walker.BaseClass;
			}
		}

		private Dictionary<string, Executable> flattenedFieldAndMethodDeclarationsByName = new Dictionary<string, Executable>();

		public void ResolveMemberIds()
		{
			if (this.memberIdsResolved) return;

			if (this.BaseClass != null)
			{
				this.BaseClass.ResolveMemberIds();
				Dictionary<string, Executable> parentDefinitions = this.BaseClass.flattenedFieldAndMethodDeclarationsByName;
				foreach (string key in parentDefinitions.Keys)
				{
					this.flattenedFieldAndMethodDeclarationsByName[key] = parentDefinitions[key];
				}
			}

			foreach (FieldDeclaration fd in this.Fields)
			{
				Executable existingItem;
				if (this.flattenedFieldAndMethodDeclarationsByName.TryGetValue(fd.NameToken.Value, out existingItem))
				{
					// TODO: definition of a field or a method? from this class or a parent?
					// TODO: check to see if this is already resolved before now.
					throw new ParserException(fd.FirstToken, "This overrides a previous definition.");
				}

				fd.MemberID = this.flattenedFieldAndMethodDeclarationsByName.Count;
				this.flattenedFieldAndMethodDeclarationsByName[fd.NameToken.Value] = fd;
			}

			foreach (FunctionDefinition fd in this.Methods)
			{
				if (!fd.IsStaticMethod)
				{
					Executable existingItem;
					if (this.flattenedFieldAndMethodDeclarationsByName.TryGetValue(fd.NameToken.Value, out existingItem))
					{
						if (existingItem is FieldDeclaration)
						{
							// TODO: again, give more information. 
							throw new ParserException(fd.FirstToken, "This field overrides a previous definition.");
						}

						// Take the old member ID it overrides.
						fd.MemberID = ((FunctionDefinition)existingItem).MemberID;
					}
					else
					{
						fd.MemberID = this.flattenedFieldAndMethodDeclarationsByName.Count;
					}

					this.flattenedFieldAndMethodDeclarationsByName[fd.NameToken.Value] = fd;
				}
			}

			this.memberIdsResolved = true;
		}
	}
}