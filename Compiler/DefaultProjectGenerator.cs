﻿using System;
using System.Collections.Generic;
using Common;

namespace Crayon
{
    public class DefaultProjectGenerator
    {
        private string originalProjectId;
        private string projectId;

        public string ProjectID { get { return this.projectId; } }

        public DefaultProjectGenerator(string projectId)
        {
            this.originalProjectId = projectId.Trim();
            this.projectId = Util.ConvertStringToAlphanumerics(this.originalProjectId);
        }

        public DefaultProjectGenerator Validate()
        {
            if (this.projectId.Length == 0) throw new InvalidOperationException("Project name did not have any alphanumeric characters.");
            if (this.projectId[0] >= '0' && this.projectId[0] <= '9') throw new InvalidOperationException("Project name cannot begin with a number.");
            if (this.projectId != this.originalProjectId)
            {
                Console.WriteLine("Warning: '" + this.originalProjectId + "' contains non-alphanumeric characters and was canonicalized into '" + this.projectId + "'");
            }
            return this;
        }

        private Dictionary<string, string> replacements;

        public Dictionary<string, FileOutput> Export()
        {
            this.replacements = new Dictionary<string, string>()
            {
                { "PROJECT_NAME", this.projectId },
                { "TITLE", this.originalProjectId },
            };

            Dictionary<string, FileOutput> output = new Dictionary<string, FileOutput>();

            SystemBitmap crayonIcon = new SystemBitmap(typeof(Util).Assembly, "icons/crayon_logo.png");
            Dictionary<int, SystemBitmap> icons = new IconSetGenerator()
                .AddOutputSize(32)
                .AddOutputSize(256)
                .GenerateWithDefaultFallback();
            output["assets/icon32.png"] = new FileOutput() { Type = FileOutputType.Image, Bitmap = icons[32] };
            output["assets/icon256.png"] = new FileOutput() { Type = FileOutputType.Image, Bitmap = icons[256] };

            System.Reflection.Assembly thisAsm = typeof(Program).Assembly;
            foreach (string file in new string[]
                {
                        "DefaultProject/BuildFile.txt|%%%PROJECT_NAME%%%.build",
                        "DefaultProject/main.txt|source/main.cry",
                        "DefaultProject/dotGitIgnore.txt|output/.gitignore",
                })
            {
                string[] parts = file.Split('|');
                string content = Util.ReadAssemblyFileText(typeof(Program).Assembly, parts[0]);
                content = this.ReplaceStrings(content);
                string outputPath = this.ReplaceStrings(parts[1]);
                output[outputPath] = new FileOutput()
                {
                    Type = FileOutputType.Text,
                    TextContent = content,
                };
            }
            Util.ReadAssemblyFileText(thisAsm, "DefaultProject/BuildFile.txt");
            return output;
        }

        private string ReplaceStrings(string text)
        {
            return Constants.DoReplacements(false, text, this.replacements);
        }
    }
}
