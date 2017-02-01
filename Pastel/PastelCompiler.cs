﻿using System;
using System.Collections.Generic;
using System.Linq;
using Pastel.Nodes;
using Common;

namespace Pastel
{
    public class PastelCompiler
    {
        public PastelCompiler(IDictionary<string, bool> constants, IInlineImportCodeLoader inlineImportCodeLoader)
        {
            this.StructDefinitions = new Dictionary<string, StructDefinition>();
            this.EnumDefinitions = new Dictionary<string, EnumDefinition>();
            this.Globals = new Dictionary<string, VariableDeclaration>();
            this.ConstantDefinitions = new Dictionary<string, VariableDeclaration>();
            this.FunctionDefinitions = new Dictionary<string, FunctionDefinition>();

            this.interpreterParser = new PastelParser(constants, inlineImportCodeLoader);
        }

        private PastelParser interpreterParser;

        internal Dictionary<string, StructDefinition> StructDefinitions { get; set; }
        internal Dictionary<string, EnumDefinition> EnumDefinitions { get; set; }
        internal Dictionary<string, VariableDeclaration> Globals { get; set; }
        internal Dictionary<string, VariableDeclaration> ConstantDefinitions { get; set; }
        internal Dictionary<string, FunctionDefinition> FunctionDefinitions { get; set; }

        internal HashSet<string> ResolvedFunctions { get; set; }
        internal HashSet<string> UnresolvedFunctions { get; set; }
        internal Queue<string> ResolutionQueue { get; set; }

        public void CompileBlobOfCode(string name, string code)
        {
            ICompilationEntity[] entities = this.interpreterParser.ParseText(name, code);
            foreach (ICompilationEntity entity in entities)
            {
                switch (entity.EntityType)
                {
                    case CompilationEntityType.FUNCTION:
                        FunctionDefinition fnDef = (FunctionDefinition)entity;
                        string functionName = fnDef.NameToken.Value;
                        if (this.FunctionDefinitions.ContainsKey(functionName))
                        {
                            throw new ParserException(fnDef.FirstToken, "Multiple definitions of function: '" + functionName + "'");
                        }
                        this.FunctionDefinitions[functionName] = fnDef;
                        break;

                    case CompilationEntityType.STRUCT:
                        StructDefinition structDef = (StructDefinition)entity;
                        string structName = structDef.NameToken.Value;
                        if (this.StructDefinitions.ContainsKey(structName))
                        {
                            throw new ParserException(structDef.FirstToken, "Multiple definitions of function: '" + structName + "'");
                        }
                        this.StructDefinitions[structName] = structDef;
                        break;

                    case CompilationEntityType.ENUM:
                        EnumDefinition enumDef = (EnumDefinition)entity;
                        string enumName = enumDef.NameToken.Value;
                        if (this.EnumDefinitions.ContainsKey(enumName))
                        {
                            throw new ParserException(enumDef.FirstToken, "Multiple definitions of function: '" + enumName + "'");
                        }
                        this.EnumDefinitions[enumName] = enumDef;
                        break;

                    case CompilationEntityType.CONSTANT:
                    case CompilationEntityType.GLOBAL:
                        VariableDeclaration assignment = (VariableDeclaration)entity;
                        string targetName = assignment.VariableName.Value;
                        Dictionary<string, VariableDeclaration> lookup = entity.EntityType == CompilationEntityType.CONSTANT
                            ? this.ConstantDefinitions
                            : this.Globals;
                        if (lookup.ContainsKey(targetName))
                        {
                            throw new ParserException(
                                assignment.FirstToken,
                                "Multiple definitions of : '" + targetName + "'");
                        }
                        lookup[targetName] = assignment;
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public void Resolve()
        {
            this.ResolveConstants();
            this.ResolveNamesAndCullUnusedCode();
        }

        private void ResolveConstants()
        {
            HashSet<string> cycleDetection = new HashSet<string>();
            foreach (EnumDefinition enumDef in this.EnumDefinitions.Values)
            {
                if (enumDef.UnresolvedValues.Count > 0)
                {
                    enumDef.DoConstantResolutions(cycleDetection, this);
                }
            }
            
            foreach (VariableDeclaration constDef in this.ConstantDefinitions.Values)
            {
                if (!(constDef.Value is InlineConstant))
                {
                    string name = constDef.VariableName.Value;
                    cycleDetection.Add(name);
                    constDef.DoConstantResolutions(cycleDetection, this);
                    cycleDetection.Remove(name);
                }
            }
        }

        private void ResolveNamesAndCullUnusedCode()
        {
            string[] globalNames = this.Globals.Keys.ToArray();
            for (int i = 0; i < globalNames.Length; ++i)
            {
                string name = globalNames[i];
                this.Globals[name] = (VariableDeclaration)this.Globals[name].ResolveNamesAndCullUnusedCode(this)[0];
            }

            this.ResolvedFunctions = new HashSet<string>();
            this.UnresolvedFunctions = new HashSet<string>();
            this.ResolutionQueue = new Queue<string>();
            this.ResolutionQueue.Enqueue("main");

            while (this.ResolutionQueue.Count > 0)
            {
                string functionName = this.ResolutionQueue.Dequeue();
                this.UnresolvedFunctions.Remove(functionName);
                this.ResolvedFunctions.Add(functionName);
                this.FunctionDefinitions[functionName].ResolveNamesAndCullUnusedCode(this);
            }
        }
    }
}
