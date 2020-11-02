﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using Serilog;

namespace ConsoleManager
{
    public class ConsoleManager<TData> : IConsoleManager
    {
        Func<string, TData> _dataReader;
        Func<TData, ILogger, IEnumerable<TData>> _program;
        Func<IEnumerable<TData>, ILogger, TData> _bestSolutionFinder;

        int _separatorLength = 20;

        ILogger _log;

        public ConsoleManager(
            Func<string, TData> dataReader, 
            Func<TData, ILogger, IEnumerable<TData>> program,
            Func<IEnumerable<TData>, ILogger, TData> bestSolutionFinder)
        {
            _dataReader = dataReader;
            _program = program;
            _bestSolutionFinder = bestSolutionFinder;

            _log = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("log.txt")
                .CreateLogger();
        }

        public void RunProgram(string[] args)
        {
            _log.Information($"{new string('=', _separatorLength)}\nProgram started at {DateTime.Now.ToShortTimeString()}.");

            CheckArgs(args);

            _log.Information("Getting path from arguments.");
            var path = GetPathFromArgs(args);

            _log.Information($"Reading and parsing input data from {path}");
            TData inputData = _dataReader.Invoke(path);

            var started = DateTime.Now;
            var stopwatch = Stopwatch.StartNew();

            _log.Information($"Starting program...");
            IEnumerable<TData> solutions = _program.Invoke(inputData, _log);

            var bestSolution = _bestSolutionFinder(solutions, _log);

            var elapsed = stopwatch.Elapsed;

            _log.Information($"Generating console result log.");
            var consoleResult = GenerateLog(started, elapsed, solutions, bestSolution);

            _log.Information($"Console dump:\n{consoleResult}"); 

            _log.Information($"Program stopped at {DateTime.Now.ToShortTimeString()}.");

            Console.WriteLine("Press any key to exit...");

            Console.ReadKey();

        }

        private void WriteToConsoleWithColorChange(TimeSpan elapsed, string consoleResult)
        {
            var consoleDefaultColor = Console.ForegroundColor;

            if (elapsed.TotalSeconds > 10)
                Console.ForegroundColor = ConsoleColor.Red;
            else
                Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine(consoleResult);

            Console.ForegroundColor = consoleDefaultColor;
        }

        private string GenerateLog(DateTime started, TimeSpan elapsed, IEnumerable<TData> solutions, TData bestSolution)
        {
            var logContent = new StringBuilder();
            var content = "";

            logContent.AppendLine($"{new string('=', _separatorLength)}");

            //header
            content = FormatHeader("Solutions found:");
            logContent.AppendLine($"{content}");

            //all solutions
            for (int i = 0; i < solutions.Count(); i++)
            {
                content = FormatSubheader($"Solution {i + 1}");
                logContent.AppendLine(content);
                logContent.AppendLine(solutions.ElementAt(i).ToString());
            }

            //best solutions
            content = FormatHeader("Best solution:");
            logContent.AppendLine($"{content }");
            logContent.AppendLine(bestSolution.ToString());

            //summary
            content = FormatHeader("Summary:");
            logContent.AppendLine($"{content}");

            logContent.AppendLine($"Program started at: {started.ToShortDateString()} {started.ToShortTimeString()}");
            logContent.AppendLine();
            logContent.AppendLine($"Elapsed time: {FormatTimeSpan(elapsed)}");
            logContent.AppendLine();
            logContent.AppendLine($"Found {solutions.Count()} solutions in total.");
            logContent.AppendLine();

            return logContent.ToString();
        }

        private string FormatHeader(string content)
        {
            var res = new StringBuilder();

            res.AppendLine();
            res.AppendLine(new string('-', _separatorLength));
            res.AppendLine(content);
            res.AppendLine(new string('-', _separatorLength));
            res.AppendLine();

            return res.ToString();
        }

        private string FormatSubheader(string content)
        {
            var res = new StringBuilder();

            res.AppendLine();
            res.AppendLine(new string('.', _separatorLength));
            res.AppendLine(content);
            res.AppendLine(new string('.', _separatorLength));
            res.AppendLine();

            return res.ToString();
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            var twoDigitFormat = "{0:00}";
            var threeDigitFormat = "{0:000}";

            var h = string.Format(twoDigitFormat, timeSpan.Hours);
            var m = string.Format(twoDigitFormat, timeSpan.Minutes);
            var s = string.Format(twoDigitFormat, timeSpan.Seconds);
            var ms = string.Format(threeDigitFormat, timeSpan.Milliseconds);

            return $"{h}:{m}:{s}.{ms}";
        }

        private string GetPathFromArgs(string[] args)
        {
            var path = args.FirstOrDefault();

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("The file path is null or empty!");

            if (!File.Exists(path))
                throw new FileNotFoundException($"For the given path >>{path}<< no file was found.");

            return path;
        }

        private void CheckArgs(string[] args)
        {
            if (args == null && !args.Any())
                throw new ArgumentException("No argument was given!");
        }
    }
}
