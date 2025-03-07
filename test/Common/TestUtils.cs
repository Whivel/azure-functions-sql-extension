﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Azure.WebJobs.Extensions.Sql.Tests.Common
{
    /// <remarks>
    /// Adapted from Microsoft.VisualStudio.TeamSystem.Data.UnitTests.UnitTestUtilities.TestDBManager
    /// </remarks>
    public static class TestUtils
    {
        internal static int ThreadId;

        /// <summary>
        /// Returns a mangled name that unique based on Prefix + Machine + Process
        /// </summary>
        public static string GetUniqueDBName(string namePrefix)
        {
            string safeMachineName = Environment.MachineName.Replace('-', '_');
            return string.Format(
                "{0}_{1}_{2}_{3}_{4}",
                namePrefix,
                safeMachineName,
                AppDomain.CurrentDomain.Id,
                Process.GetCurrentProcess().Id,
                Interlocked.Increment(ref ThreadId));
        }

        /// <summary>
        /// Creates a IDbCommand and calls ExecuteNonQuery against the connection.
        /// </summary>
        /// <param name="connection">The connection.  This must be opened.</param>
        /// <param name="commandText">The scalar T-SQL command.</param>
        /// <param name="catchException">Optional exception handling.  Pass back 'true' to handle the
        /// exception, 'false' to throw. If Null is passed in then all exceptions are thrown.</param>
        /// <returns>The number of rows affected</returns>
        public static int ExecuteNonQuery(
            IDbConnection connection,
            string commandText,
            Predicate<Exception> catchException = null)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (commandText == null)
            {
                throw new ArgumentNullException(nameof(commandText));
            }

            using (IDbCommand cmd = connection.CreateCommand())
            {
                try
                {

                    cmd.CommandText = commandText;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 60000; // Increase from default 30s to prevent timeouts while connecting to Azure SQL DB
                    Console.WriteLine($"Executing non-query {commandText}");
                    return cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    if (catchException == null || !catchException(ex))
                    {
                        throw;
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Creates a IDbCommand and calls ExecuteScalar against the connection.
        /// </summary>
        /// <param name="connection">The connection.  This must be opened.</param>
        /// <param name="commandText">The scalar T-SQL command.</param>
        /// <param name="catchException">Optional exception handling.  Pass back 'true' to handle the
        /// exception, 'false' to throw. If Null is passed in then all exceptions are thrown.</param>
        /// <returns>The scalar result</returns>
        public static object ExecuteScalar(
            IDbConnection connection,
            string commandText,
            Predicate<Exception> catchException = null)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (commandText == null)
            {
                throw new ArgumentNullException(nameof(commandText));
            }

            using (IDbCommand cmd = connection.CreateCommand())
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.CommandType = CommandType.Text;
                    Console.WriteLine($"Executing scalar {commandText}");
                    return cmd.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    if (catchException == null || !catchException(ex))
                    {
                        throw;
                    }
                }

            }

            return null;
        }

        /// <summary>
        /// Retries the specified action, waiting for the specified duration in between each attempt
        /// </summary>
        /// <param name="action">The action to run</param>
        /// <param name="retryCount">The max number of retries to attempt</param>
        /// <param name="waitDurationMs">The duration in milliseconds between each attempt</param>
        /// <exception cref="AggregateException">Aggregate of all exceptions thrown if all retries failed</exception>
        public static void Retry(Action action, int retryCount = 3, int waitDurationMs = 10000)
        {
            var exceptions = new List<Exception>();
            while (true)
            {
                try
                {
                    action();
                    break;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    retryCount--;
                    if (retryCount == 0)
                    {
                        throw new AggregateException($"Action failed all retries", exceptions);
                    }
                    Console.WriteLine($"Error running action, retrying after {waitDurationMs}ms. {retryCount} retries left. {ex}");
                    Thread.Sleep(waitDurationMs);
                }
            }
        }

        /// <summary>
        /// Removes spaces, tabs and new lines from the JSON string.
        /// </summary>
        /// <param name="jsonStr">The json string for trimming</param>
        public static string CleanJsonString(string jsonStr)
        {
            return jsonStr.Trim().Replace(" ", "").Replace(Environment.NewLine, "");
        }
    }
}
