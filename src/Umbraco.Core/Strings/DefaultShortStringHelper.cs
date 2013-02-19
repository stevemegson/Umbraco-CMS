﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Umbraco.Core.Configuration;

namespace Umbraco.Core.Strings
{
    /// <summary>
    /// New default implementation of string functions for short strings such as aliases or url segments.
    /// </summary>
    /// <remarks>
    /// <para>Not optimized to work on large bodies of text.</para>
    /// <para>Meant to replace <c>LegacyShortStringHelper</c> where/when backward compatibility is not an issue.</para>
    /// <para>Full-unicode support is probably not so good.</para>
    /// <para>NOTE: pre-filters run _before_ the string is re-encoded.</para>
    /// </remarks>
    internal class DefaultShortStringHelper : IShortStringHelper
    {
        #region Ctor and vars

        /// <summary>
        /// Freezes the helper so it can prevents its configuration from being modified.
        /// </summary>
        /// <remarks>Will be called by <c>ShortStringHelperResolver</c> when resolution freezes.</remarks>
        public void Freeze()
        {
            _frozen = true;
        }

        // see notes for CleanAsciiString
        //// beware! the order is quite important here!
        //const string ValidStringCharactersSource = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        //readonly static char[] ValidStringCharacters;

        private CultureInfo _defaultCulture = CultureInfo.InvariantCulture;
        private bool _frozen;
        private readonly Dictionary<CultureInfo, Dictionary<CleanStringType, HelperConfig>> _configs = new Dictionary<CultureInfo, Dictionary<CleanStringType, HelperConfig>>();

        // see notes for CleanAsciiString
        //static DefaultShortStringHelper()
        //{
        //    ValidStringCharacters = ValidStringCharactersSource.ToCharArray();
        //}

        #endregion

        #region Configuration

        private void EnsureNotFrozen()
        {
            if (_frozen)
                throw new InvalidOperationException("Cannot configure the helper once it is frozen.");            
        }

        public DefaultShortStringHelper WithDefaultCulture(CultureInfo culture)
        {
            EnsureNotFrozen();
            _defaultCulture = culture;
            return this;
        }

        public DefaultShortStringHelper WithConfig(
            Func<string, string> preFilter = null, 
            bool breakTermsOnUpper = true, bool allowLeadingDigits = false, bool allowUnderscoreInTerm = false)
        {
            return WithConfig(_defaultCulture, CleanStringType.RoleMask,
                preFilter, breakTermsOnUpper, allowLeadingDigits, allowUnderscoreInTerm);
        }

        public DefaultShortStringHelper WithConfig(CleanStringType stringRole,
            Func<string, string> preFilter = null,
            bool breakTermsOnUpper = true, bool allowLeadingDigits = false, bool allowUnderscoreInTerm = false)
        {
            return WithConfig(_defaultCulture, stringRole,
                preFilter, breakTermsOnUpper, allowLeadingDigits, allowUnderscoreInTerm);
        }

        public DefaultShortStringHelper WithConfig(CultureInfo culture, CleanStringType stringRole,
            Func<string, string> preFilter = null,
            bool breakTermsOnUpper = true, bool allowLeadingDigits = false, bool allowUnderscoreInTerm = false)
        {
            EnsureNotFrozen();
            if (!_configs.ContainsKey(culture))
                _configs[culture] = new Dictionary<CleanStringType, HelperConfig>();
            _configs[culture][stringRole] = new HelperConfig(preFilter, breakTermsOnUpper, allowLeadingDigits, allowUnderscoreInTerm);
            return this;
        }

        internal sealed class HelperConfig
        {
            private HelperConfig()
            {
                PreFilter = null;
                BreakTermsOnUpper = true;
                AllowLeadingDigits = false;
            }

            public HelperConfig(Func<string, string> preFilter, bool breakTermsOnUpper, bool allowLeadingDigits, bool allowUnderscoreInTerm)
                : this()
            {
                PreFilter = preFilter;
                BreakTermsOnUpper = breakTermsOnUpper;
                AllowLeadingDigits = allowLeadingDigits;
                AllowUnderscoreInTerm = allowUnderscoreInTerm;
            }

            public Func<string, string> PreFilter { get; private set; }
            public bool BreakTermsOnUpper { get; private set; }
            public bool AllowLeadingDigits { get; private set; }
            public bool AllowUnderscoreInTerm { get; private set; }

            public static readonly HelperConfig Empty = new HelperConfig();
        }

        private HelperConfig GetConfig(CleanStringType stringType, CultureInfo culture)
        {
            Dictionary<CleanStringType, HelperConfig> config;
            if (_configs.ContainsKey(culture))
            {
                config = _configs[culture];
                if (config.ContainsKey(stringType)) // have we got a config for _that_ role?
                    return config[stringType];
                if (config.ContainsKey(CleanStringType.RoleMask)) // have we got a generic config for _all_ roles?
                    return config[CleanStringType.RoleMask];
            }
            else if (_configs.ContainsKey(_defaultCulture))
            {
                config = _configs[_defaultCulture];
                if (config.ContainsKey(stringType)) // have we got a config for _that_ role?
                    return config[stringType];
                if (config.ContainsKey(CleanStringType.RoleMask)) // have we got a generic config for _all_ roles?
                    return config[CleanStringType.RoleMask];
            }

            return HelperConfig.Empty;
        }

        #endregion

        #region JavaScript

        const string CsfaJsValidCharacters = "_abcdefghijklmnopqrstuvwxyz1234567890";
        const string CsfaJsInvalidFirstCharacters = "01234567890_";

        private const string CsfsaJsFormat = @"
var UMBRACO_FORCE_SAFE_ALIAS = {0};
var UMBRACO_FORCE_SAFE_ALIAS_VALIDCHARS = '{1}';
var UMBRACO_FORCE_SAFE_ALIAS_INVALID_FIRST_CHARS = '{2}';

function safeAlias(alias) {{
    if (UMBRACO_FORCE_SAFE_ALIAS) {{
        var safeAlias = '';
        var aliasLength = alias.length;
        for (var i = 0; i < aliasLength; i++) {{
            currentChar = alias.substring(i, i + 1);
            if (UMBRACO_FORCE_SAFE_ALIAS_VALIDCHARS.indexOf(currentChar.toLowerCase()) > -1) {{
                // check for camel (if previous character is a space, we'll upper case the current one
                if (safeAlias == '' && UMBRACO_FORCE_SAFE_ALIAS_INVALID_FIRST_CHARS.indexOf(currentChar.toLowerCase()) > 0) {{ 
                    currentChar = '';
                }} else {{
                    // first char should always be lowercase (camel style)
                    if (safeAlias.length == 0)
                        currentChar = currentChar.toLowerCase();

                    if (i < aliasLength - 1 && safeAlias != '' && alias.substring(i - 1, i) == ' ')
                        currentChar = currentChar.toUpperCase();

                    safeAlias += currentChar;
                }}
            }}
        }}

        alias = safeAlias;
    }}
    return alias;
}}

function getSafeAlias(id, value, immediate, callback) {{
    callback(safeAlias(value));
}}

function validateSafeAlias(id, value, immediate, callback) {{
    callback(value == safeAlias(value));
}}
";

        /// <summary>
        /// Gets the JavaScript code defining functions safeAlias(alias) and isSafeAlias(alias).
        /// </summary>
        public string CleanStringForSafeAliasJavaScriptCode
        {
            get
            {
                return string.Format(CsfsaJsFormat,
                    UmbracoSettings.ForceSafeAliases ? "true" : "false", CsfaJsValidCharacters, CsfaJsInvalidFirstCharacters);
            }
        }

        #endregion

        #region IShortStringHelper CleanFor...

        /// <summary>
        /// Cleans a string to produce a string that can safely be used in an alias.
        /// </summary>
        /// <param name="text">The text to filter.</param>
        /// <returns>The safe alias.</returns>
        /// <remarks>
        /// <para>The string will be cleaned in the context of the default culture.</para>
        /// <para>Safe aliases are Ascii only.</para>
        /// </remarks>
        public string CleanStringForSafeAlias(string text)
        {
            return CleanString(text, CleanStringType.Ascii | CleanStringType.CamelCase | CleanStringType.Alias);
        }

        /// <summary>
        /// Cleans a string, in the context of a specified culture, to produce a string that can safely be used in an alias.
        /// </summary>
        /// <param name="text">The text to filter.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>The safe alias.</returns>
        /// <remarks>
        /// <para>Safe aliases are Ascii only.</para>
        /// </remarks>
        public string CleanStringForSafeAlias(string text, CultureInfo culture)
        {
            return CleanString(text, CleanStringType.Ascii | CleanStringType.CamelCase | CleanStringType.Alias, culture);
        }

        /// <summary>
        /// Cleans a string to produce a string that can safely be used in an url segment.
        /// </summary>
        /// <param name="text">The text to filter.</param>
        /// <returns>The safe url segment.</returns>
        /// <remarks>
        /// <para>The string will be cleaned in the context of the default culture.</para>
        /// <para>Url segments are Ascii only (no accents...).</para>
        /// </remarks>
        public string CleanStringForUrlSegment(string text)
        {
            return CleanString(text, CleanStringType.Ascii | CleanStringType.LowerCase | CleanStringType.Url, '-');
        }

        /// <summary>
        /// Cleans a string, in the context of a specified culture, to produce a string that can safely be used in an url segment.
        /// </summary>
        /// <param name="text">The text to filter.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>The safe url segment.</returns>
        /// <remarks>
        /// <para>Url segments are Ascii only (no accents...).</para>
        /// </remarks>
        public string CleanStringForUrlSegment(string text, CultureInfo culture)
        {
            return CleanString(text, CleanStringType.Ascii | CleanStringType.LowerCase | CleanStringType.Url, '-', culture);
        }

        #endregion

        #region CleanString

        // MS rules & guidelines:
        // - Do capitalize both characters of two-character acronyms, except the first word of a camel-cased identifier.
        //     eg "DBRate" (pascal) or "ioHelper" (camel) - "specialDBRate" (pascal) or "specialIOHelper" (camel)
        // - Do capitalize only the first character of acronyms with three or more characters, except the first word of a camel-cased identifier.
        //     eg "XmlWriter (pascal) or "htmlReader" (camel) - "SpecialXmlWriter" (pascal) or "specialHtmlReader" (camel)
        // - Do not capitalize any of the characters of any acronyms, whatever their length, at the beginning of a camel-cased identifier.
        //     eg "xmlWriter" or "dbWriter" (camel)
        //
        // Our additional stuff:
        // - Leading digits are removed.
        // - Many consecutive separators are folded into one unique separator.

        const byte StateBreak = 1;
        const byte StateUp = 2;
        const byte StateWord = 3;
        const byte StateAcronym = 4;

        /// <summary>
        /// Cleans a string.
        /// </summary>
        /// <param name="text">The text to clean.</param>
        /// <param name="stringType">A flag indicating the target casing and encoding of the string. By default, 
        /// strings are cleaned up to camelCase and Ascii.</param>
        /// <returns>The clean string.</returns>
        /// <remarks>The string is cleaned in the context of the default culture.</remarks>
        public string CleanString(string text, CleanStringType stringType)
        {
            return CleanString(text, stringType, char.MinValue, _defaultCulture);
        }

        /// <summary>
        /// Cleans a string, using a specified separator.
        /// </summary>
        /// <param name="text">The text to clean.</param>
        /// <param name="stringType">A flag indicating the target casing and encoding of the string. By default, 
        /// strings are cleaned up to camelCase and Ascii.</param>
        /// <param name="separator">The separator.</param>
        /// <returns>The clean string.</returns>
        /// <remarks>The string is cleaned in the context of the default culture.</remarks>
        public string CleanString(string text, CleanStringType stringType, char separator)
        {
            return CleanString(text, stringType, separator, _defaultCulture);
        }

        /// <summary>
        /// Cleans a string in the context of a specified culture.
        /// </summary>
        /// <param name="text">The text to clean.</param>
        /// <param name="stringType">A flag indicating the target casing and encoding of the string. By default, 
        /// strings are cleaned up to camelCase and Ascii.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>The clean string.</returns>
        public string CleanString(string text, CleanStringType stringType, CultureInfo culture)
        {
            return CleanString(text, stringType, char.MinValue, culture);
        }

        /// <summary>
        /// Cleans a string in the context of a specified culture, using a specified separator.
        /// </summary>
        /// <param name="text">The text to clean.</param>
        /// <param name="stringType">A flag indicating the target casing and encoding of the string. By default, 
        /// strings are cleaned up to camelCase and Ascii.</param>
        /// <param name="separator">The separator.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>The clean string.</returns>
        public string CleanString(string text, CleanStringType stringType, char separator, CultureInfo culture)
        {
            var config = GetConfig(stringType & CleanStringType.RoleMask, culture);
            return CleanString(text, stringType, separator, culture, config);
        }

        /// <summary>
        /// Cleans a string in the context of a specified culture, using a specified separator and configuration.
        /// </summary>
        /// <param name="text">The text to clean.</param>
        /// <param name="stringType">A flag indicating the target casing and encoding of the string. By default, 
        /// strings are cleaned up to camelCase and Ascii.</param>
        /// <param name="separator">The separator.</param>
        /// <param name="culture">The culture.</param>
        /// <param name="config">The configuration.</param>
        /// <returns>The clean string.</returns>
        private string CleanString(string text, CleanStringType stringType, char separator, CultureInfo culture, HelperConfig config)
        {
            // apply defaults
            if ((stringType & CleanStringType.CaseMask) == CleanStringType.None)
                stringType |= CleanStringType.CamelCase;
            if ((stringType & CleanStringType.CodeMask) == CleanStringType.None)
                stringType |= CleanStringType.Ascii;

            var codeType = stringType & CleanStringType.CodeMask;

            // apply pre-filter
            if (config.PreFilter != null)
                text = config.PreFilter(text);

            // apply replacements
            //if (config.Replacements != null)
            //    text = ReplaceMany(text, config.Replacements);

            // recode
            text = Recode(text, stringType);

            // clean
            switch (codeType)
            {
                case CleanStringType.Ascii:
                    // see note below - don't use CleanAsciiString
                    //text = CleanAsciiString(text, stringType, separator);
                    //break;
                case CleanStringType.Utf8:
                    text = CleanUtf8String(text, stringType, separator, culture, config);
                    break;
                case CleanStringType.Unicode:
                    throw new NotImplementedException("DefaultShortStringHelper does not handle unicode yet.");
                default:
                    throw new ArgumentOutOfRangeException("stringType");
            }

            return text;
        }

        // however proud I can be of that subtle, ascii-optimized code,
        // benchmarking shows it is an order of magnitude slower that the utf8 version
        // don't use it - keep it here should anyone be tempted to micro-optimize again...
        //
        // beware, it has bugs that are fixed in CleanUtf8String but I'm not going to
        // bugfix commented code....

        /*
        internal string CleanAsciiString(string text)
        {
            return CleanAsciiString(text, CleanStringType.CamelCase, char.MinValue);
        }

        internal string CleanAsciiString(string text, CleanStringType caseType, char separator)
        {
            int opos = 0, ipos = 0;
            var state = StateBreak;

            caseType &= CleanStringType.CaseMask;

            //switch (caseType)
            //{
            //    case CleanStringType.LowerCase:
            //        input = text.ToLowerInvariant().ToCharArray();
            //        break;
            //    case CleanStringType.UpperCase:
            //        input = text.ToUpperInvariant().ToCharArray();
            //        break;
            //    default:
            //        input =  text.ToCharArray();
            //        break;
            //}
            // if we apply global ToUpper or ToLower to text here
            // then we cannot break words on uppercase chars
            var input = text;

            // because we shouldn't be adding any extra char
            // it's faster to use an array than a StringBuilder
            var ilen = input.Length;
            var output = new char[ilen];

            Func<string, string> termFilter = null;

            for (var i = 0; i < ilen; i++)
            {
                var idx = ValidStringCharacters.IndexOf(input[i]);

                switch (state)
                {
                    case StateBreak:
                        if (idx >= 0 && (opos > 0 || idx < 26 || idx >= 36))
                        {
                            ipos = i;
                            if (opos > 0 && separator != char.MinValue)
                                output[opos++] = separator;
                            state = idx < 36 ? StateWord : StateUp;
                        }
                        break;

                    case StateWord:
                        if (idx < 0 || (_breakTermsOnUpper && idx >= 36))
                        {
                            CopyAsciiTerm(input, ipos, output, ref opos, i - ipos, caseType, termFilter, false);
                            ipos = i;
                            state = idx < 0 ? StateBreak : StateUp;
                            if (state != StateBreak && separator != char.MinValue)
                                output[opos++] = separator;
                        }
                        break;

                    case StateAcronym:
                        if (idx < 36)
                        {
                            CopyAsciiTerm(input, ipos, output, ref opos, i - ipos, caseType, termFilter, true);
                            ipos = i;
                            state = idx < 0 ? StateBreak : StateWord;
                            if (state != StateBreak && separator != char.MinValue)
                                output[opos++] = separator;
                        }
                        break;

                    case StateUp:
                        if (idx >= 0)
                        {
                            state = idx < 36 ? StateWord : StateAcronym;
                        }
                        else
                        {
                            CopyAsciiTerm(input, ipos, output, ref opos, 1, caseType, termFilter, false);
                            state = StateBreak;
                        }
                        break;

                    default:
                        throw new Exception("Invalid state.");
                }
            }

            //Console.WriteLine("xx: ({0}) {1}, {2}, {3}", state, input.Length, ipos, opos);
            switch (state)
            {
                case StateBreak:
                    break;

                case StateWord:
                    CopyAsciiTerm(input, ipos, output, ref opos, input.Length - ipos, caseType, termFilter, false);
                    break;

                case StateAcronym:
                case StateUp:
                    CopyAsciiTerm(input, ipos, output, ref opos, input.Length - ipos, caseType, termFilter, true);
                    break;

                default:
                    throw new Exception("Invalid state.");
            }

            return new string(output, 0, opos);
        }

        internal void CopyAsciiTerm(string input, int ipos, char[] output, ref int opos, int len,
            CleanStringType caseType, Func<string, string> termFilter, bool isAcronym)
        {
            var term = input.Substring(ipos, len);
            ipos = 0;

            if (termFilter != null)
            {
                term = termFilter(term);
                len = term.Length;
            }

            if (isAcronym)
            {
                if (caseType == CleanStringType.CamelCase && len <= 2 && opos > 0)
                    caseType = CleanStringType.Unchanged;
                else if (caseType == CleanStringType.PascalCase && len <= 2)
                    caseType = CleanStringType.Unchanged;
            }

            int idx;
            switch (caseType)
            {
                //case CleanStringType.LowerCase:
                //case CleanStringType.UpperCase:
                case CleanStringType.Unchanged:
                    term.CopyTo(ipos, output, opos, len);
                    opos += len;
                    break;

                case CleanStringType.LowerCase:
                    for (var i = ipos; i < ipos + len; i++)
                    {
                        idx = ValidStringCharacters.IndexOf(term[i]);
                        output[opos++] = ValidStringCharacters[idx >= 36 ? idx - 36 : idx];
                    }
                    break;

                case CleanStringType.UpperCase:
                    for (var i = ipos; i < ipos + len; i++)
                    {
                        idx = ValidStringCharacters.IndexOf(term[i]);
                        output[opos++] = ValidStringCharacters[idx < 26 ? idx + 36 : idx];
                    }
                    break;

                case CleanStringType.CamelCase:
                    idx = ValidStringCharacters.IndexOf(term[ipos]);
                    if (opos == 0)
                        output[opos++] = ValidStringCharacters[idx >= 36 ? idx - 36 : idx];
                    else
                        output[opos++] = ValidStringCharacters[idx < 26 ? idx + 36 : idx];
                    for (var i = ipos + 1; i < ipos + len; i++)
                    {
                        idx = ValidStringCharacters.IndexOf(term[i]);
                        output[opos++] = ValidStringCharacters[idx >= 36 ? idx - 36 : idx];
                    }
                    break;

                case CleanStringType.PascalCase:
                    idx = ValidStringCharacters.IndexOf(term[ipos]);
                    output[opos++] = ValidStringCharacters[idx < 26 ? idx + 36 : idx];
                    for (var i = ipos + 1; i < ipos + len; i++)
                    {
                        idx = ValidStringCharacters.IndexOf(term[i]);
                        output[opos++] = ValidStringCharacters[idx >= 36 ? idx - 36 : idx];
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException("caseType");
            }
        }
        */

        // that's the default code that will work for utf8 strings
        // will not handle unicode, though

        internal string CleanUtf8String(string text)
        {
            return CleanUtf8String(text, CleanStringType.CamelCase, char.MinValue, _defaultCulture, HelperConfig.Empty);
        }

        internal string CleanUtf8String(string text, CleanStringType caseType, char separator, CultureInfo culture, HelperConfig config)
        {
            int opos = 0, ipos = 0;
            var state = StateBreak;

            caseType &= CleanStringType.CaseMask;

            // if we apply global ToUpper or ToLower to text here
            // then we cannot break words on uppercase chars
            var input = text;

            // because we shouldn't be adding any extra char
            // it's faster to use an array than a StringBuilder
            var ilen = input.Length;
            var output = new char[ilen];

            //var termFilter = config.TermFilter;

            for (var i = 0; i < ilen; i++)
            {
                var c = input[i];
                var isDigit = char.IsDigit(c);
                var isUpper = char.IsUpper(c); // false for digits, symbols...
                var isLower = char.IsLower(c); // false for digits, symbols...
                var isUnder = config.AllowUnderscoreInTerm && c == '_';
                var isTerm = char.IsLetterOrDigit(c) || isUnder;

                switch (state)
                {
                    case StateBreak:
                        if (isTerm && (opos > 0 || (!isUnder && (config.AllowLeadingDigits || !isDigit))))
                        {
                            ipos = i;
                            if (opos > 0 && separator != char.MinValue)
                                output[opos++] = separator;
                            state = isUpper ? StateUp : StateWord;
                        }
                        break;

                    case StateWord:
                        if (!isTerm || (config.BreakTermsOnUpper && isUpper))
                        {
                            CopyUtf8Term(input, ipos, output, ref opos, i - ipos, caseType, culture, /*termFilter,*/ false);
                            ipos = i;
                            state = isTerm ? StateUp : StateBreak;
                            if (state != StateBreak && separator != char.MinValue)
                                output[opos++] = separator;
                        }
                        break;

                    case StateAcronym:
                        if (!isTerm || isLower || isDigit)
                        {
                            CopyUtf8Term(input, ipos, output, ref opos, i - ipos, caseType, culture, /*termFilter,*/ true);
                            ipos = i;
                            state = isTerm ? StateWord : StateBreak;
                            if (state != StateBreak && separator != char.MinValue)
                                output[opos++] = separator;
                        }
                        break;

                    case StateUp:
                        if (isTerm)
                        {
                            state = isUpper ? StateAcronym : StateWord;
                        }
                        else
                        {
                            CopyUtf8Term(input, ipos, output, ref opos, 1, caseType, culture, /*termFilter,*/ false);
                            state = StateBreak;
                        }
                        break;

                    default:
                        throw new Exception("Invalid state.");
                }
            }

            switch (state)
            {
                case StateBreak:
                    break;

                case StateWord:
                    CopyUtf8Term(input, ipos, output, ref opos, input.Length - ipos, caseType, culture, /*termFilter,*/ false);
                    break;

                case StateAcronym:
                case StateUp:
                    CopyUtf8Term(input, ipos, output, ref opos, input.Length - ipos, caseType, culture, /*termFilter,*/ true);
                    break;

                default:
                    throw new Exception("Invalid state.");
            }

            return new string(output, 0, opos);
        }

        internal void CopyUtf8Term(string input, int ipos, char[] output, ref int opos, int len,
            CleanStringType caseType, CultureInfo culture, /*Func<string, string> termFilter,*/ bool isAcronym)
        {
            var term = input.Substring(ipos, len);
            ipos = 0;

            //if (termFilter != null)
            //{
            //    term = termFilter(term);
            //    len = term.Length;
            //}

            if (isAcronym)
            {
                if (caseType == CleanStringType.CamelCase && len <= 2 && opos > 0)
                    caseType = CleanStringType.Unchanged;
                else if (caseType == CleanStringType.PascalCase && len <= 2)
                    caseType = CleanStringType.Unchanged;
            }

            char c;
            switch (caseType)
            {
                //case CleanStringType.LowerCase:
                //case CleanStringType.UpperCase:
                case CleanStringType.Unchanged:
                    term.CopyTo(ipos, output, opos, len);
                    opos += len;
                    break;

                case CleanStringType.LowerCase:
                    term.ToLower(culture).CopyTo(ipos, output, opos, len);
                    opos += len;
                    break;

                case CleanStringType.UpperCase:
                    term.ToUpper(culture).CopyTo(ipos, output, opos, len);
                    opos += len;
                    break;

                case CleanStringType.CamelCase:
                    c = term[ipos++];
                    output[opos] = opos++ == 0 ? char.ToLower(c, culture) : char.ToUpper(c, culture);
                    if (len > 1)
                        term.ToLower(culture).CopyTo(ipos, output, opos, len - 1);
                    opos += len - 1;
                    break;

                case CleanStringType.PascalCase:
                    c = term[ipos++];
                    output[opos++] = char.ToUpper(c, culture);
                    if (len > 1)
                        term.ToLower(culture).CopyTo(ipos, output, opos, len - 1);
                    opos += len - 1;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("caseType");
            }
        }

        #endregion

        #region SplitPascalCasing

        /// <summary>
        /// Splits a Pascal-cased string into a phrase separated by a separator.
        /// </summary>
        /// <param name="text">The text to split.</param>
        /// <param name="separator">The separator, which defaults to a whitespace.</param>
        /// <returns>The splitted text.</returns>
        /// <remarks>Supports Utf8 and Ascii strings, not Unicode strings.</remarks>
        public string SplitPascalCasing(string text, char separator)
        {
            var input = text.ToCharArray();
            var output = new char[input.Length * 2];
            var opos = 0;
            var a = input.Length > 0 ? input[0] : char.MinValue;
            var upos = char.IsUpper(a) ? 1 : 0;

            for (var i = 1; i < input.Length; i++)
            {
                var c = input[i];
                if (char.IsUpper(c))
                {
                    output[opos++] = a;
                    if (upos == 0)
                    {
                        if (opos > 0)
                            output[opos++] = separator;
                        upos = i + 1;
                    }
                }
                else
                {
                    if (upos > 0)
                    {
                        if (upos < i && opos > 0)
                            output[opos++] = separator;
                        upos = 0;
                    }
                    output[opos++] = a;
                }
                a = c;
            }
            if (a != char.MinValue)
                output[opos++] = a;
            return new string(output, 0, opos);
        }

        #endregion

        #region Recode

        /// <summary>
        /// Returns a new string containing only characters within the specified code type.
        /// </summary>
        /// <param name="text">The string to filter.</param>
        /// <param name="stringType">The string type.</param>
        /// <returns>The filtered string.</returns>
        /// <remarks>If <paramref name="stringType"/> is not <c>Unicode</c> then non-utf8 characters are
        /// removed. If it is <c>Ascii</c> we try to do some intelligent replacement of accents, etc.</remarks>
        public string Recode(string text, CleanStringType stringType)
        {
            var codeType = stringType & CleanStringType.CodeMask;

            // unicode to utf8 or ascii: just remove the unicode chars
            // utf8 to ascii: try to be clever and replace some chars

            // what's the point?
            if (codeType == CleanStringType.Unicode)
                return text;

            return codeType == CleanStringType.Utf8 
                ? RemoveNonUtf8(text) 
                : Utf8ToAsciiConverter.ToAsciiString(text);
        }

        private string RemoveNonUtf8(string text)
        {
            var len = text.Length;
            var output = new char[len]; // we won't be adding chars
            int opos = 0;

            for (var ipos = 0; ipos < len; ipos++)
            {
                var c = text[ipos];
                if (char.IsSurrogate(c))
                    ipos++;
                else
                    output[opos++] = c;
            }
            return new string(output, 0, opos);
        }

        #endregion

        #region ReplaceMany

        /// <summary>
        /// Returns a new string in which all occurences of specified strings are replaced by other specified strings.
        /// </summary>
        /// <param name="text">The string to filter.</param>
        /// <param name="replacements">The replacements definition.</param>
        /// <returns>The filtered string.</returns>
        public string ReplaceMany(string text, IDictionary<string, string> replacements)
        {
            // Have done various tests, implementing my own "super fast" state machine to handle 
            // replacement of many items, or via regexes, but on short strings and not too
            // many replacements (which prob. is going to be our case) nothing can beat this...
            // (at least with safe and checked code -- we don't want unsafe/unchecked here)

            // Note that it will do chained-replacements ie replaced items can be replaced
            // in turn by another replacement (ie the order of replacements is important)

            return replacements.Aggregate(text, (current, kvp) => current.Replace(kvp.Key, kvp.Value));
        }

        #endregion
    }
}
