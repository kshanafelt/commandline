using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine.Infrastructure;
using CSharpx;

namespace CommandLine.Text
{
    public class HelpTextFactory
    {
        private const int DefaultMaximumLength = 78;

        private static readonly Lazy<HeadingInfo> HeadingDefault = new Lazy<HeadingInfo>(() => HeadingInfo.Default);
        private static readonly Lazy<CopyrightInfo> CopyrightDefault = new Lazy<CopyrightInfo>(() => CopyrightInfo.Default);
        private static readonly Lazy<SentenceBuilder> SentenceBuilderDefault = new Lazy<SentenceBuilder>(SentenceBuilder.CreateDefault);

        private string heading;
        private string copyright;
        private SentenceBuilder sentenceBuilder;
        private int? maximumDisplayWidth;
        private bool additionalNewLineAfterOption = true;

        public HelpTextFactory()
        {
            AddEnumValuesToHelpText = false;
        }

        /// <summary>
        /// Gets or sets the heading string.
        /// You can directly assign a <see cref="CommandLine.Text.HeadingInfo"/> instance.
        /// <code>null</code> values throw an <see cref="ArgumentNullException(string)"/> exception.
        /// </summary>
        public string Heading
        {
            get { return heading ?? (heading = HeadingDefault.Value); }

            set
            {
                if (value == null) throw new ArgumentNullException("value");
                heading = value;
            }
        }

        /// <summary>
        /// Gets or sets the copyright string.
        /// You can directly assign a <see cref="CommandLine.Text.CopyrightInfo"/> instance.
        /// <code>null</code> values throw an <see cref="ArgumentNullException(string)"/> exception.
        /// </summary>
        public string Copyright
        {
            get { return copyright ?? (copyright = CopyrightDefault.Value); }

            set
            {
                if (value == null) throw new ArgumentNullException("value");
                copyright = value;
            }
        }

        public bool AlwaysPrintCopyright { get; set; }

        /// <summary>
        /// Gets the <see cref="SentenceBuilder"/> instance specified in constructor.
        /// <code>null</code> values throw an <see cref="ArgumentNullException(string)"/> exception.
        /// </summary>
        public SentenceBuilder SentenceBuilder
        {
            get { return sentenceBuilder ?? (sentenceBuilder = SentenceBuilderDefault.Value); }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                sentenceBuilder = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum width of the display.  This determines word wrap when displaying the text.
        /// </summary>
        /// <value>The maximum width of the display.</value>
        public int MaximumDisplayWidth
        {
            get { return maximumDisplayWidth ?? DefaultMaximumLength; }
            set { maximumDisplayWidth = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the format of options should contain dashes.
        /// It modifies behavior of <see cref="HelpText.AddOptions{T}(ParserResult{T})"/> method.
        /// <code>null</code> values will use the value given to one of the <see cref="Build{T}"/> methods,
        /// or <code>false</code>, for Build methods that do not take a addDashesToOption parameter.
        /// </summary>
        public bool? AddDashesToOption { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to add an additional line after the description of the specification.
        /// Set to <code>true</code> by default.
        /// </summary>
        public bool AdditionalNewLineAfterOption
        {
            get { return additionalNewLineAfterOption; }
            set { additionalNewLineAfterOption = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to add the values of an enum after the description of the specification.
        /// </summary>
        public bool AddEnumValuesToHelpText { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="CommandLine.Text.HelpText"/> class using common defaults.
        /// </summary>
        /// <returns>
        /// An instance of <see cref="CommandLine.Text.HelpText"/> class.
        /// </returns>
        /// <param name='parserResult'>The <see cref="CommandLine.ParserResult{T}"/> containing the instance that collected command line arguments parsed with <see cref="CommandLine.Parser"/> class.</param>
        /// <param name='onError'>A delegate used to customize the text block of reporting parsing errors text block.</param>
        /// <param name='onExample'>A delegate used to customize <see cref="CommandLine.Text.Example"/> model used to render text block of usage examples.</param>
        /// <param name="verbsIndex">If true the output style is consistent with verb commands (no dashes), otherwise it outputs options.</param>
        /// <remarks>The parameter <paramref name="verbsIndex"/> is not ontly a metter of formatting, it controls whether to handle verbs or options.</remarks>
        public HelpText Build<T>(
            ParserResult<T> parserResult,
            Func<HelpText, HelpText> onError,
            Func<Example, Example> onExample,
            bool verbsIndex = false)
        {
            var auto = new HelpText
            {
                Heading = HeadingInfo.Default,
                Copyright = CopyrightInfo.Default,
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = !verbsIndex
            };

            IList<Error> errors;

            if (onError != null && parserResult.Tag == ParserResultType.NotParsed)
            {
                errors = ((NotParsed<T>) parserResult).Errors.ToList();

                if (errors.OnlyMeaningfulOnes().Any())
                    auto = onError(auto);
            }
            else
            {
                errors = Enumerable.Empty<Error>().ToList();
            }

            ReflectionHelper.GetAttribute<AssemblyLicenseAttribute>()
                .Do(license => license.AddToHelpText(auto, true));

            var usageAttr = ReflectionHelper.GetAttribute<AssemblyUsageAttribute>();
            var usageLines = HelpText.RenderUsageTextAsLines(parserResult, onExample).ToMaybe();

            if (usageAttr.IsJust() || usageLines.IsJust())
            {
                var usageHeading = auto.SentenceBuilder.UsageHeadingText();
                if (usageHeading.Length > 0)
                    auto.AddPreOptionsLine(usageHeading);
            }

            usageAttr.Do(
                usage => usage.AddToHelpText(auto, true));

            usageLines.Do(
                lines => auto.AddPreOptionsLines(lines));

            if ((verbsIndex && parserResult.TypeInfo.Choices.Any())
                || errors.Any(e => e.Tag == ErrorType.NoVerbSelectedError))
            {
                auto.AddDashesToOption = false;
                auto.AddVerbs(parserResult.TypeInfo.Choices.ToArray());
            }
            else
                auto.AddOptions(parserResult);

            return auto;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CommandLine.Text.HelpText"/> class,
        /// automatically handling verbs or options scenario.
        /// </summary>
        /// <param name='parserResult'>The <see cref="CommandLine.ParserResult{T}"/> containing the instance that collected command line arguments parsed with <see cref="CommandLine.Parser"/> class.</param>
        /// <returns>
        /// An instance of <see cref="CommandLine.Text.HelpText"/> class.
        /// </returns>
        /// <remarks>This feature is meant to be invoked automatically by the parser, setting the HelpWriter property
        /// of <see cref="CommandLine.ParserSettings"/>.</remarks>
        public HelpText Build<T>(ParserResult<T> parserResult)
        {
            if (parserResult.Tag != ParserResultType.NotParsed)
                throw new ArgumentException("Excepting NotParsed<T> type.", "parserResult");

            var errors = ((NotParsed<T>)parserResult).Errors.ToList();

            // todo fix new HelpText so that it uses a BuildHeader method
            if (errors.Any(e => e.Tag == ErrorType.VersionRequestedError))
                return BuildHeading();

            if (errors.All(e => e.Tag != ErrorType.HelpVerbRequestedError))
                return Build(parserResult, current => DefaultParsingErrorsHandler(parserResult, current), e => e);

            var err = errors.OfType<HelpVerbRequestedError>().Single();
            var pr = new NotParsed<object>(TypeInfo.Create(err.Type), Enumerable.Empty<Error>());
            return err.Matched
                ? Build(pr, current => DefaultParsingErrorsHandler(pr, current), e => e)
                : Build(parserResult, current => DefaultParsingErrorsHandler(parserResult, current), e => e, true);
        }

        /// <summary>
        /// Supplies a default parsing error handler implementation.
        /// </summary>
        /// <param name='parserResult'>The <see cref="CommandLine.ParserResult{T}"/> containing the instance that collected command line arguments parsed with <see cref="CommandLine.Parser"/> class.</param>
        /// <param name="current">The <see cref="CommandLine.Text.HelpText"/> instance.</param>
        public HelpText DefaultParsingErrorsHandler<T>(ParserResult<T> parserResult, HelpText current)
        {
            if (parserResult == null) throw new ArgumentNullException("parserResult");
            if (current == null) throw new ArgumentNullException("current");

            if (((NotParsed<T>)parserResult).Errors.OnlyMeaningfulOnes().Empty())
                return current;

            var errors = HelpText.RenderParsingErrorsTextAsLines(parserResult,
                current.SentenceBuilder.FormatError,
                current.SentenceBuilder.FormatMutuallyExclusiveSetErrors,
                2).ToList(); // indent with two spaces
            if (errors.Empty())
                return current;

            return current
                .AddPreOptionsLine(
                    string.Concat(Environment.NewLine, current.SentenceBuilder.ErrorsHeadingText()))
                .AddPreOptionsLines(errors);
        }

        public HelpText BuildHeading()
        {
            return AlwaysPrintCopyright? 
                new HelpText(Heading, Copyright).AddPreOptionsLine(Environment.NewLine) : 
                new HelpText(Heading).AddPreOptionsLine(Environment.NewLine);
        }
    }
}
