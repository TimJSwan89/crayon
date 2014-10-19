﻿using System;
using System.Collections.Generic;

namespace Crayon.Translator.Python
{
	internal class PythonSystemFunctionTranslator : AbstractSystemFunctionTranslator
	{
		public PythonSystemFunctionTranslator()
			: base()
		{ }

		protected override void TranslateBeginFrame(List<string> output)
		{
			throw new Exception("This code path should be optimized out of the python translation.");
		}

		protected override void TranslateCast(List<string> output, ParseTree.Expression typeValue, ParseTree.Expression expression)
		{
			this.Translator.TranslateExpression(output, expression);
		}

		protected override void TranslateComment(List<string> output, ParseTree.Expression commentValue)
		{
#if DEBUG
			output.Add("# " + ((ParseTree.StringConstant)commentValue).Value);
#endif
		}

		// Not safe for dictionaries that can contain a value of None.
		protected override void TranslateDictionaryContains(List<string> output, ParseTree.Expression dictionary, ParseTree.Expression key)
		{
			output.Add("(");
			this.Translator.TranslateExpression(output, dictionary);
			output.Add(".get(");
			this.Translator.TranslateExpression(output, key);
			output.Add(", None) != None)");
		}

		protected override void TranslateDictionaryGet(List<string> output, ParseTree.Expression dictionary, ParseTree.Expression key, ParseTree.Expression defaultValue)
		{
			this.Translator.TranslateExpression(output, dictionary);
			output.Add(".get(");
			this.Translator.TranslateExpression(output, key);
			output.Add(", ");
			this.Translator.TranslateExpression(output, defaultValue);
			output.Add(")");
		}

		protected override void TranslateDictionaryGetKeys(List<string> output, ParseTree.Expression dictionary)
		{
			output.Add("list(");
			this.Translator.TranslateExpression(output, dictionary);
			output.Add(".keys())");
		}

		protected override void TranslateDictionaryGetValues(List<string> output, ParseTree.Expression dictionary)
		{
			output.Add("list(");
			this.Translator.TranslateExpression(output, dictionary);
			output.Add(".values())");
		}

		protected override void TranslateDictionaryRemove(List<string> output, ParseTree.Expression dictionary, ParseTree.Expression key)
		{
			this.Translator.TranslateExpression(output, dictionary);
			output.Add(".pop(");
			this.Translator.TranslateExpression(output, key);
			output.Add(")");
		}

		protected override void TranslateDictionarySet(List<string> output, ParseTree.Expression dict, ParseTree.Expression key, ParseTree.Expression value)
		{
			this.Translator.TranslateExpression(output, dict);
			output.Add("[");
			this.Translator.TranslateExpression(output, key);
			output.Add("] = ");
			this.Translator.TranslateExpression(output, value);
		}

		protected override void TranslateDictionarySize(List<string> output, ParseTree.Expression dictionary)
		{
			output.Add("len(");
			this.Translator.TranslateExpression(output, dictionary);
			output.Add(")");
		}

		protected override void TranslateExponent(List<string> output, ParseTree.Expression baseNum, ParseTree.Expression powerNum)
		{
			output.Add("float(");
			this.Translator.TranslateExpression(output, baseNum);
			output.Add(" ** ");
			this.Translator.TranslateExpression(output, powerNum);
			output.Add(")");
		}

		protected override void TranslateGetProgramData(List<string> output)
		{
			output.Add("program_data[0]");
		}

		protected override void TranslateGetRawByteCodeString(List<string> output, string theString)
		{
			output.Add("\"");
			output.Add(theString);
			output.Add("\"");
		}

		// TODO: this is supposed to be in the pygame platform stuff.
		// Also, implement each switch result as an abstract function
		protected override void TranslateInsertFrameworkCode(string tab, List<string> output, string id)
		{
			switch (id)
			{
				case "ff_arctan2":
					output.Add("v_output = [" + (int)Types.FLOAT + ", math.atan2(v_y[1], v_x[1])]");
					break;

				case "ff_blit_image":
					output.Add("_global_vars['virtual_screen'].blit(v_image[1][1], (v_x[1], v_y[1]))");
					break;

				case "ff_cos":
					output.Add("v_output = [" + (int)Types.FLOAT + ", math.cos(v_x[1])]");
					break;

				case "ff_current_time":
					output.Add("v_output = [" + (int)Types.FLOAT + ", time.time()]");
					break;

				case "ff_download_image":
					output.Add("download_image_impl(v_key[1], v_url[1])");
					break;

				case "ff_draw_ellipse":
					output.Add("_PDE(_global_vars['virtual_screen'], (v_red[1], v_green[1], v_blue[1]), _PR(v_left[1], v_top[1], v_width[1], v_height[1]))");
					break;

				case "ff_draw_line":
					output.Add("_PDL(_global_vars['virtual_screen'], (v_red[1], v_green[1], v_blue[1]), (v_x1[1], v_y1[1]), (v_x2[1], v_y2[1]), v_width[1])");
					break;

				case "ff_draw_rectangle":
					// TODO: alpha?
					output.Add("_PDR(_global_vars['virtual_screen'], (v_red[1], v_green[1], v_blue[1]), _PR(v_x[1], v_y[1], v_width[1], v_height[1]))");
					break;

				case "ff_fill_screen":
					output.Add("_global_vars['virtual_screen'].fill((v_red[1], v_green[1], v_blue[1]))");
					break;

				case "ff_flip_image":
					output.Add("v_output = _pygame_flip_image(v_img[1], v_x[1], v_y[1])");
					break;

				case "ff_floor":
					output.Add("v_output = v_build_integer(int(v_value[1]) if (v_value[1] >= 0) else int(math.floor(v_value[1])))");
					break;

				case "ff_get_events":
					output.Add("v_output = _pygame_pump_events()");
					break;

				case "ff_get_image":
					output.Add("v_output = get_image_impl(v_key[1])");
					break;

				case "ff_get_image_height":
					output.Add("v_output = v_build_integer(v_value[1][1].get_height())");
					break;

				case "ff_get_image_width":
					output.Add("v_output = v_build_integer(v_value[1][1].get_width())");
					break;

				case "ff_initialize_game":
					output.Add("platform_begin(v_fps[1])");
					break;

				case "ff_initialize_screen":
					output.Add("v_output = _pygame_initialize_screen(v_width[1], v_height[1], None)");
					break;

				case "ff_initialize_screen_scaled":
					output.Add("v_output = _pygame_initialize_screen(v_width[1], v_height[1], (v_pwidth[1], v_pheight[1]))");
					break;

				case "ff_is_image_loaded":
					output.Add("v_output = v_VALUE_TRUE");
					break;

				case "ff_parse_int":
					output.Add("v_output = [" + (int)Types.INTEGER + ", int(v_value[1])]");
					break;

				case "ff_print":
					output.Add("print(v_string1)");
					break;

				case "ff_random":
					output.Add("v_output = [" + (int)Types.FLOAT + ", random.random()]");
					break;

				case "ff_set_title":
					output.Add("pygame.display.set_caption(v_value)");
					break;

				case "ff_sin":
					output.Add("v_output = [" + (int)Types.FLOAT + ", math.sin(v_x[1])]");
					break;

				default:
					throw new NotImplementedException();
			}
		}

		protected override void TranslateInt(List<string> output, ParseTree.Expression value)
		{
			output.Add("int(");
			this.Translator.TranslateExpression(output, value);
			output.Add(")");
		}

		protected override void TranslateListConcat(List<string> output, ParseTree.Expression listA, ParseTree.Expression listB)
		{
			output.Add("(");
			this.Translator.TranslateExpression(output, listA);
			output.Add(" + ");
			this.Translator.TranslateExpression(output, listB);
			output.Add(")");
		}

		protected override void TranslateListGet(List<string> output, ParseTree.Expression list, ParseTree.Expression index)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add("[");
			this.Translator.TranslateExpression(output, index);
			output.Add("]");
		}

		protected override void TranslateListInsert(List<string> output, ParseTree.Expression list, ParseTree.Expression index, ParseTree.Expression value)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add(".insert(");
			this.Translator.TranslateExpression(output, index);
			output.Add(", ");
			this.Translator.TranslateExpression(output, value);
			output.Add(")");
		}

		protected override void TranslateListJoin(List<string> output, ParseTree.Expression list, ParseTree.Expression sep)
		{
			this.Translator.TranslateExpression(output, sep);
			output.Add(".join(");
			this.Translator.TranslateExpression(output, list);
			output.Add(")");
		}

		protected override void TranslateListLastIndex(List<string> output, ParseTree.Expression list)
		{
			output.Add("-1");
		}

		protected override void TranslateListLength(List<string> output, ParseTree.Expression list)
		{
			output.Add("len(");
			this.Translator.TranslateExpression(output, list);
			output.Add(")");
		}

		protected override void TranslateListNew(List<string> output, ParseTree.Expression length)
		{
			output.Add("([None]" + this.Shorten(" * "));
			this.Translator.TranslateExpression(output, length);
			output.Add(")");
		}

		protected override void TranslateListPop(List<string> output, ParseTree.Expression list)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add(".pop()");
		}

		protected override void TranslateListPush(List<string> output, ParseTree.Expression list, ParseTree.Expression value)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add(".append(");
			this.Translator.TranslateExpression(output, value);
			output.Add(")");
		}

		protected override void TranslateListRemoveAt(List<string> output, ParseTree.Expression list, ParseTree.Expression index)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add(".pop(");
			this.Translator.TranslateExpression(output, index);
			output.Add(")");
		}

		protected override void TranslateListReverseInPlace(List<string> output, ParseTree.Expression listVar)
		{
			this.Translator.TranslateExpression(output, listVar);
			output.Add(" = ");
			this.Translator.TranslateExpression(output, listVar);
			output.Add("[::-1]");
		}

		protected override void TranslateListSet(List<string> output, ParseTree.Expression list, ParseTree.Expression index, ParseTree.Expression value)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add("[");
			this.Translator.TranslateExpression(output, index);
			output.Add("] = ");
			this.Translator.TranslateExpression(output, value);
		}

		protected override void TranslateListShuffleInPlace(List<string> output, ParseTree.Expression list)
		{
			output.Add("random.shuffle(");
			this.Translator.TranslateExpression(output, list);
			output.Add(")");
		}

		protected override void TranslateNewArray(List<string> output, ParseTree.Expression type, ParseTree.Expression size)
		{
			output.Add("[None] * ");
			this.Translator.TranslateExpression(output, size);
		}

		protected override void TranslatePauseForFrame(List<string> output)
		{
			output.Add("_pygame_end_of_frame()");
		}

		protected override void TranslatePrint(List<string> output, ParseTree.Expression message)
		{
			output.Add("print(");
			this.Translator.TranslateExpression(output, message);
			output.Add(")");
		}

		protected override void TranslateRegisterTicker(List<string> output)
		{
			throw new Exception("This code path should be optimized out of the python translation.");
		}

		protected override void TranslateRegisterTimeout(List<string> output)
		{
			throw new Exception("This code path should be optimized out of the python translation.");
		}

		protected override void TranslateSetProgramData(List<string> output, ParseTree.Expression programData)
		{
			output.Add("program_data[0] = ");
			this.Translator.TranslateExpression(output, programData);
		}

		protected override void TranslateStringCast(List<string> output, ParseTree.Expression thing, bool strongCast)
		{
			output.Add("str(");
			this.Translator.TranslateExpression(output, thing);
			output.Add(")");
		}

		protected override void TranslateStringCharAt(List<string> output, ParseTree.Expression stringValue, ParseTree.Expression index)
		{
			this.Translator.TranslateExpression(output, stringValue);
			output.Add("[");
			this.Translator.TranslateExpression(output, index);
			output.Add("]");
		}

		protected override void TranslateStringContains(List<string> output, ParseTree.Expression haystack, ParseTree.Expression needle)
		{
			output.Add("(");
			this.Translator.TranslateExpression(output, needle);
			output.Add(" in ");
			this.Translator.TranslateExpression(output, haystack);
			output.Add(")");
		}

		protected override void TranslateStringEndsWith(List<string> output, ParseTree.Expression stringExpr, ParseTree.Expression findMe)
		{
			this.Translator.TranslateExpression(output, stringExpr);
			output.Add(".endswith(");
			this.Translator.TranslateExpression(output, findMe);
			output.Add(")");
		}

		protected override void TranslateStringFromCode(List<string> output, ParseTree.Expression characterCode)
		{
			output.Add("wrappedChr(");
			this.Translator.TranslateExpression(output, characterCode);
			output.Add(")");
		}

		protected override void TranslateStringIndexOf(List<string> output, ParseTree.Expression haystack, ParseTree.Expression needle)
		{
			this.Translator.TranslateExpression(output, haystack);
			output.Add(".find(");
			this.Translator.TranslateExpression(output, needle);
			output.Add(")");
		}

		protected override void TranslateStringLength(List<string> output, ParseTree.Expression stringValue)
		{
			output.Add("len(");
			this.Translator.TranslateExpression(output, stringValue);
			output.Add(")");
		}

		protected override void TranslateStringLower(List<string> output, ParseTree.Expression stringValue)
		{
			this.Translator.TranslateExpression(output, stringValue);
			output.Add(".lower()");
		}

		protected override void TranslateStringParseFloat(List<string> output, ParseTree.Expression stringValue)
		{
			output.Add("float(");
			this.Translator.TranslateExpression(output, stringValue);
			output.Add(")");
		}

		protected override void TranslateStringParseInt(List<string> output, ParseTree.Expression value)
		{
			output.Add("int(");
			this.Translator.TranslateExpression(output, value);
			output.Add(")");
		}

		protected override void TranslateStringReplace(List<string> output, ParseTree.Expression stringValue, ParseTree.Expression findMe, ParseTree.Expression replaceWith)
		{
			this.Translator.TranslateExpression(output, stringValue);
			output.Add(".replace(");
			this.Translator.TranslateExpression(output, findMe);
			output.Add(", ");
			this.Translator.TranslateExpression(output, replaceWith);
			output.Add(")");
		}

		protected override void TranslateStringReverse(List<string> output, ParseTree.Expression stringValue)
		{
			this.Translator.TranslateExpression(output, stringValue);
			output.Add("[::-1]");
		}

		protected override void TranslateStringSplit(List<string> output, ParseTree.Expression stringExpr, ParseTree.Expression sep)
		{
			this.Translator.TranslateExpression(output, stringExpr);
			output.Add(".split(");
			this.Translator.TranslateExpression(output, sep);
			output.Add(")");
		}

		protected override void TranslateStringStartsWith(List<string> output, ParseTree.Expression stringExpr, ParseTree.Expression findMe)
		{
			this.Translator.TranslateExpression(output, stringExpr);
			output.Add(".startswith(");
			this.Translator.TranslateExpression(output, findMe);
			output.Add(")");
		}

		protected override void TranslateStringTrim(List<string> output, ParseTree.Expression stringValue)
		{
			this.Translator.TranslateExpression(output, stringValue);
			output.Add(".strip()");
		}

		protected override void TranslateStringUpper(List<string> output, ParseTree.Expression stringValue)
		{
			this.Translator.TranslateExpression(output, stringValue);
			output.Add(".upper()");
		}

		protected override void TranslateUnregisterTicker(List<string> output)
		{
			throw new Exception("This code path should be optimized out of the python translation.");
		}

		protected override void TranslateUnsafeFloatDivision(List<string> output, ParseTree.Expression numerator, ParseTree.Expression denominator)
		{
			output.Add("1.0 * ");
			this.Translator.TranslateExpression(output, numerator);
			output.Add(" / ");
			this.Translator.TranslateExpression(output, denominator);
		}

		protected override void TranslateUnsafeIntegerDivision(List<string> output, ParseTree.Expression numerator, ParseTree.Expression denominator)
		{
			this.Translator.TranslateExpression(output, numerator);
			output.Add(" // ");
			this.Translator.TranslateExpression(output, denominator);
		}
	}
}
