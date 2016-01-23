﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crayon.Translator.JavaScript
{
	class JavaScriptPlatform : AbstractPlatform
	{
		private string jsFolderPrefix;

		public JavaScriptPlatform(bool isMin, string jsFolderPrefix)
			: base(PlatformId.JAVASCRIPT_CANVAS, LanguageId.JAVASCRIPT, isMin, new JavaScriptTranslator(), new JavaScriptSystemFunctionTranslator(), null, null)
		{
			this.jsFolderPrefix = jsFolderPrefix;
		}

		public override bool IsAsync { get { return true; } }
		public override bool SupportsListClear { get { return false; } }
		public override bool IsStronglyTyped { get { return false; } }
		public override bool UseFixedListArgConstruction { get { return false; } }
		public override bool ImagesLoadInstantly { get { return false; } }
		public override bool ScreenBlocksExecution { get { return false; } }
		public override bool IsArraySameAsList { get { return true; } }
		public override string PlatformShortId { get { return "javascript"; } }

		public override string GeneratedFilesFolder { get { return "generated_resources"; } }

		public override Dictionary<string, FileOutput> Package(
			BuildContext buildContext,
			string projectId,
			Dictionary<string, ParseTree.Executable[]> finalCode,
			List<string> filesToCopyOver,
			ICollection<ParseTree.StructDefinition> structDefinitions,
			string inputFolder,
			SpriteSheetBuilder spriteSheet)
		{
			Dictionary<string, FileOutput> output = new Dictionary<string, FileOutput>();

			Dictionary<string, string> replacements = new Dictionary<string, string>()
			{
				{ "JS_FILE_PREFIX", "'" + this.jsFolderPrefix + "'" },
				{ "PROJECT_ID", projectId },
			};

			output["index.html"] = new FileOutput()
			{
				Type = FileOutputType.Text,
				TextContent = this.GenerateHtmlFile()
			};

			List<string> codeJs = new List<string>();

			foreach (string jsFile in new string[] {
				"game_code.js",
				"input.js",
				"drawing.js",
				"sound.js",
				"file_io.js",
				"fake_disk.js",
				"http.js",
				"interpreter_helpers.js",
			})
			{
				codeJs.Add(Minify(Util.ReadFileInternally("Translator/JavaScript/Browser/" + jsFile)));
				codeJs.Add(this.Translator.NL);
			}

			foreach (string component in finalCode.Keys)
			{
				this.Translator.Translate(codeJs, finalCode[component]);
				codeJs.Add(this.Translator.NL);
			}

			string codeJsText = Constants.DoReplacements(string.Join("", codeJs), replacements);

			if (this.IsMin)
			{
				codeJsText = JavaScriptMinifier.Minify(codeJsText);
			}

			output["code.js"] = new FileOutput()
			{
				Type = FileOutputType.Text,
				TextContent = codeJsText
			};



			output["bytecode.js"] = new FileOutput()
			{
				Type = FileOutputType.Text,
				TextContent = string.Join("\n", new string[] {
					"var CRAYON = {};",
					"CRAYON.getByteCode = function() {",
					"\treturn \"" + this.Context.ByteCodeString + "\";",
					"};",
					""
				})
			};

			HashSet<string> binaryFileTypes = new HashSet<string>(
				"JPG PNG GIF JPEG BMP MP3 OGG".Split(' '));

			Dictionary<string, string> textResources = new Dictionary<string, string>();

			foreach (string file in filesToCopyOver)
			{
				string[] parts = file.Split('.');
				bool isBinary = file.Length > 1 && binaryFileTypes.Contains(parts[parts.Length - 1].ToUpperInvariant());

				if (isBinary)
				{
					output[file] = new FileOutput()
					{
						Type = FileOutputType.Copy,
						RelativeInputPath = file
					};
					textResources[file] = null;
				}
				else
				{
					textResources[file] = Util.ReadFileExternally(System.IO.Path.Combine(inputFolder, file), false);
				}
			}

			if (textResources.Count > 0)
			{
				output["resources.js"] = new FileOutput()
				{
					TextContent = BuildTextResourcesCodeFile(textResources),
					Type = FileOutputType.Text
				};
			}

			return output;
		}


		private static readonly HashSet<string> KNOWN_BINARY_FILES = new HashSet<string>()
		{
			"MP3",
			"WAV",
			"OGG",
			"BMP",
			"GIF",
			"PNG",
			"TIFF",
			"JPEG",
			"JPG"
		};

		private string BuildTextResourcesCodeFile(Dictionary<string, string> files)
		{
			List<string> output = new List<string>();
			output.Add("R.resources = {\n");
			string[] keys = files.Keys.OrderBy<string, string>(s => s.ToLowerInvariant()).ToArray();
			for (int i = 0; i < keys.Length; ++i)
			{
				string filename = keys[i];
				string[] filenamePieces = filename.Split('.');
				string extension = filenamePieces[filenamePieces.Length - 1].ToUpperInvariant();
				if (KNOWN_BINARY_FILES.Contains(extension))
				{
					continue;
				}
				output.Add("\t\"");
				output.Add(filename.Replace('\\', '/'));
				output.Add("\": ");

				if (files[filename] != null)
				{
					output.Add(Util.ConvertStringValueToCode(files[filename], true));
				}
				else
				{
					output.Add("null");
				}
				if (i != keys.Length - 1)
				{
					output.Add(",");
				}
				output.Add("\n");
			}
			output.Add("};\n");
			return string.Join("", output);
		}

		private string Minify(string data)
		{
			if (!this.IsMin)
			{
				return data;
			}

			string[] lines = data.Split('\n');
			List<string> output = new List<string>();
			int counter = -1;
			foreach (string rawLine in lines)
			{
				string line = rawLine.Trim();
				int commentLoc = line.IndexOf("//");
				if (commentLoc != -1)
				{
					line = line.Substring(0, commentLoc);
				}

				line = line
					.Replace(" = ", "=")
					.Replace(", ", ",")
					.Replace(" + ", "+")
					.Replace(" == ", "==")
					.Replace(" += ", "+=")
					.Replace(" -= ", "-=")
					.Replace(" - ", "-")
					.Replace(" / ", "/")
					.Replace("function (", "function(")
					.Replace(") {", "){");
				if (counter-- > 0)
				{
					line += "\r\n";
				}
				output.Add(line);

			}

			return string.Join("", output);
		}

		public string GenerateHtmlFile()
		{
			List<string> output = new List<string>();

			output.Add("<!doctype html>\n<html lang=\"en-US\" dir=\"ltr\">");
			output.Add("<head>");
			output.Add("<meta charset=\"utf-8\">");
			output.Add("<title>Crayon JS output</title>");
			output.Add("<style type=\"text/css\"> body { background-color:#000; text-align:center; }</style>");
			output.Add("<script type=\"text/javascript\" src=\"bytecode.js\"></script>");
			output.Add("<script type=\"text/javascript\" src=\"code.js\"></script>");
			output.Add("<script type=\"text/javascript\" src=\"resources.js\"></script>");
			output.Add("</head>");
			output.Add("<body onload=\"v_main()\">");
			output.Add(Util.ReadFileInternally("Translator/JavaScript/Browser/game_host_html.txt"));
			output.Add("</body>");
			output.Add("</html>");

			return string.Join(this.IsMin ? "" : "\n", output);
		}
	}
}
