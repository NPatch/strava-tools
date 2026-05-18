using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using static System.MemoryExtensions;

namespace StravaTools.Utilities.CommandLine
{
    public static class OptionExtensions
    {
        public static void NotANumber(OptionResult argumentResult)
        {
            for (var i = 0; i < argumentResult.Tokens.Count; i++)
            {
                var token = argumentResult.Tokens[i];

                if (token.Type == TokenType.Option)
                {
                    if (!int.TryParse(token.Value, out int number))
                    {
                        argumentResult.AddError($"Token {token.Value} is not a valid number");
                    }
                }
            }
        }

        public static Option<string> AcceptOnlyValidNumbers(
            this Option<string> argument)
        {
            argument.Validators.Add(NotANumber);

            return argument;
        }

        public static Option<int[]> AcceptOnlyValidNumbers(
            this Option<int[]> option)
        {
            option.Validators.Add(NotANumber);

            return option;
        }

        public static Option<string[]> AcceptOnlyValidNumbers(
            this Option<string[]> option)
        {
            option.Validators.Add(NotANumber);

            return option;
        }

        public static Option<string> AcceptOnlyValidDateTimes(
            this Option<string> option)
        {
            option.Validators.Add(CommonExtensions.NotASupportedDateTime);

            return option;
        }

        public static Option<T> AcceptLegalFilePathsOnly<T>(this Option<T> argument)
        {
            argument.Validators.Add(result =>
            {
                var invalidPathChars = Path.GetInvalidPathChars();

                for (var i = 0; i < result.Tokens.Count; i++)
                {
                    var token = result.Tokens[i];

                    // File class no longer check invalid character
                    // https://blogs.msdn.microsoft.com/jeremykuhne/2018/03/09/custom-directory-enumeration-in-net-core-2-1/
                    var invalidCharactersIndex = token.Value.IndexOfAny(invalidPathChars);

                    if (invalidCharactersIndex >= 0)
                    {
                        result.AddError($"Invalid characters in path: {token.Value[invalidCharactersIndex]}");
                    }
                }
            });

            return argument;
        }

        public static Option<T> AcceptOnlyFromAmong<T>(this Option<T> option,
            params string[] values)
        {
            if (values != null && values.Length > 0)
            {
                option.Validators.Add(UnrecognizedOptionError);
            }

            return option;

            void UnrecognizedOptionError(OptionResult optionResult)
            {
                for (var i = 0; i < optionResult.Tokens.Count; i++)
                {
                    var token = optionResult.Tokens[i];

                    if (token.Type == TokenType.Option)
                    {
                        if (Array.IndexOf(values, token.Value) < 0)
                        {
                            optionResult.AddError($"Unrecognized option {token.Value}: {optionResult.Option.HelpName}");
                        }
                    }
                }
            }
        }
    }

    public static class ArgumentExtensions
    {
        public static void NotANumber(ArgumentResult argumentResult)
        {
            for (var i = 0; i < argumentResult.Tokens.Count; i++)
            {
                var token = argumentResult.Tokens[i];

                if (token.Type == TokenType.Argument)
                {
                    if (!int.TryParse(token.Value, out int number))
                    {
                        argumentResult.AddError($"Token {token.Value} is not a valid number");
                    }
                }
            }
        }

        public static void NotANumber<T>(ArgumentResult argumentResult)
            where T : INumber<T>
        {
            for (var i = 0; i < argumentResult.Tokens.Count; i++)
            {
                var token = argumentResult.Tokens[i];

                if (token.Type == TokenType.Argument)
                {
                    if (!T.TryParse(token.Value, System.Globalization.NumberStyles.Number, null, out T number))
                    {
                        argumentResult.AddError($"Token {token.Value} is not a valid number");
                    }
                }
            }
        }

        public static Argument<string[]> AcceptOnlyValidNumbers(
            this Argument<string[]> argument)
        {
            argument.Validators.Add(NotANumber);
            return argument;
        }

        public static Argument<int[]> AcceptOnlyValidNumbers(
            this Argument<int[]> argument)
        {
            argument.Validators.Add(NotANumber);

            return argument;
        }

        public static Argument<long> AcceptOnlyValidNumbers(
            this Argument<long> argument)
        {
            argument.Validators.Add(NotANumber<long>);

            return argument;
        }

        public static Argument AcceptOnlyValidNumbers(
            this Argument argument)
        {
            argument.Validators.Add(NotANumber);
            return argument;
        }

        public static Argument<string> AcceptOnlyValidDateTimes(
            this Argument<string> argument)
        {
            argument.Validators.Add(CommonExtensions.NotASupportedDateTime);

            return argument;
        }

        public static Argument<DirectoryInfo> AcceptLegalFilePathsOnly<DirectoryInfo>(this Argument<DirectoryInfo> argument)
        {
            argument.Validators.Add(result =>
            {
                var invalidPathChars = Path.GetInvalidPathChars();

                for (var i = 0; i < result.Tokens.Count; i++)
                {
                    var token = result.Tokens[i];

                    // File class no longer check invalid character
                    // https://blogs.msdn.microsoft.com/jeremykuhne/2018/03/09/custom-directory-enumeration-in-net-core-2-1/
                    var invalidCharactersIndex = token.Value.IndexOfAny(invalidPathChars);

                    if (invalidCharactersIndex >= 0)
                    {
                        result.AddError($"Invalid characters in path: {token.Value[invalidCharactersIndex]}");
                    }
                }
            });

            return argument;
        }

        public static Argument<FileInfo> AcceptExtensions<FileInfo>(this Argument<FileInfo> argument, params string[] extensions)
        {
            if (extensions != null && extensions.Length > 0)
            {
                argument.Validators.Add(result =>
                {
                    for (var i = 0; i < result.Tokens.Count; i++)
                    {
                        if (result.Tokens[i].Type != TokenType.Argument) continue;
                        System.IO.FileInfo token = new System.IO.FileInfo(result.Tokens[i].Value);

                        if (!extensions.Contains(token.Extension))
                        {
                            result.AddError($"Invalid file type provided: {token.Extension}");
                        }
                    }
                });
            }
            return argument;
        }

        public static Argument<FileInfo> AcceptLegalFullPathsOnly<FileInfo>(this Argument<FileInfo> argument)
        {
            argument.Validators.Add(result =>
            {
                for (var i = 0; i < result.Tokens.Count; i++)
                {
                    if (result.Tokens[i].Type != TokenType.Argument) continue;

                    ReadOnlySpan<char> content = result.Tokens[i].Value;
                    int last_separator_index = content.LastIndexOf(Path.DirectorySeparatorChar);
                    ReadOnlySpan<char> path = content.Slice(0, last_separator_index + 1);
                    ReadOnlySpan<char> filename = content.Slice(last_separator_index + 1, content.Length - (last_separator_index + 1));

                    {
                        DirectoryInfo di = new DirectoryInfo(path.ToString());
                        var invalidPathChars = Path.GetInvalidPathChars();
                        var invalidFilenameChars = Path.GetInvalidFileNameChars();

                        string non_rooted_path_string = Path.GetRelativePath(Path.GetPathRoot(path).ToString(), path.ToString());
                        ReadOnlySpan<char> non_rooted_path = null;

                        if (Path.IsPathFullyQualified(path))
                        {
                            non_rooted_path = non_rooted_path_string.AsSpan();
                        }

                        // File class no longer check invalid character
                        // https://blogs.msdn.microsoft.com/jeremykuhne/2018/03/09/custom-directory-enumeration-in-net-core-2-1/
                        var invalidCharactersIndex = path.IndexOfAny(invalidPathChars);

                        if (invalidCharactersIndex >= 0)
                        {
                            result.AddError($"Invalid characters in filepath: {path[invalidCharactersIndex]}");
                        }
                        else if (invalidCharactersIndex < 0
                                && non_rooted_path != ReadOnlySpan<char>.Empty && non_rooted_path.Length > 0)
                        {
                            char[] separators = new char[]
                            {
                                Path.DirectorySeparatorChar,
                                Path.VolumeSeparatorChar,
                                Path.AltDirectorySeparatorChar
                            };

                            foreach (var chunk in path.Split(separators))
                            {
                                if (path[chunk].SequenceEqual("name"))
                                    Console.WriteLine("The string contains `name`");
                            }

                            ReadOnlySpan<char> st = separators.AsSpan<char>().Slice(0, 1);
                            foreach (var range in non_rooted_path.Split(st))
                            {
                                ReadOnlySpan<char> section = non_rooted_path.Slice(range.Start.Value, range.End.Value - range.Start.Value);
                                if (section.Length == 0) continue;
                                invalidCharactersIndex = section.IndexOfAny(invalidFilenameChars);
                                if (invalidCharactersIndex >= 0)
                                {
                                    result.AddError($"Invalid characters in filepath: {non_rooted_path[invalidCharactersIndex]}");
                                }
                            }
                        }
                    }

                    {
                        var invalidFilenameChars = Path.GetInvalidFileNameChars();

                        // File class no longer check invalid character
                        // https://blogs.msdn.microsoft.com/jeremykuhne/2018/03/09/custom-directory-enumeration-in-net-core-2-1/
                        var invalidCharactersIndex = filename.IndexOfAny(invalidFilenameChars);

                        if (invalidCharactersIndex >= 0)
                        {
                            result.AddError($"Invalid characters in filename: {filename[invalidCharactersIndex]}");
                        }
                    }
                }
            });

            return argument;
        }

        public static Argument<T> AcceptOnlyFromAmong<T>(this Argument<T> argument,
            params string[] values)
        {
            if (values != null && values.Length > 0)
            {
                argument.Validators.Add(UnrecognizedArgumentError);
            }

            return argument;

            void UnrecognizedArgumentError(ArgumentResult argumentResult)
            {
                for (var i = 0; i < argumentResult.Tokens.Count; i++)
                {
                    var token = argumentResult.Tokens[i];

                    if (token.Type == TokenType.Option)
                    {
                        if (Array.IndexOf(values, token.Value) < 0)
                        {
                            argumentResult.AddError($"Unrecognized argument {token.Value}: {argumentResult.Argument.HelpName}");
                        }
                    }
                }
            }
        }
    }

    public static class CommonExtensions
    {
        static string[] formats = {"M/d/yyyy h:mm:ss tt", "M/d/yyyy h:mm tt",
                   "MM/dd/yyyy hh:mm:ss", "M/d/yyyy h:mm:ss",
                   "M/d/yyyy hh:mm tt", "M/d/yyyy hh tt",
                   "M/d/yyyy h:mm", "M/d/yyyy h:mm",
                   "MM/dd/yyyy hh:mm", "M/dd/yyyy hh:mm", "M-d-yyyy",
                    "MM-dd-yyyy"};

        public static void NotASupportedDateTime<T>(T result) where T : SymbolResult
        {
            for (var i = 0; i < result.Tokens.Count; i++)
            {
                var token = result.Tokens[i];
                {
                    if (!DateTime.TryParseExact(token.Value, formats,
                              new CultureInfo("en-US"),
                              DateTimeStyles.None,
                              out DateTime dateValue))
                    {
                        result.AddError($"Token {token.Value} is not a valid datetime");
                    }
                }
            }
        }
    }
}