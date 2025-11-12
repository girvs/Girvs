// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Girvs.EntityFrameworkCore.Migrations
{
    [DebuggerStepThrough]
    internal static class Check
    {
        [ContractAnnotation("value:null => halt")]
        public static T NotNull<T>([NoEnumeration] T value,
            [InvokerParameterName] [JetBrains.Annotations.NotNull]
            string parameterName)
        {
#pragma warning disable IDE0041 // Use 'is null' check
            if (ReferenceEquals(value, null))
#pragma warning restore IDE0041 // Use 'is null' check
            {
                NotEmpty(parameterName, nameof(parameterName));

                throw new ArgumentNullException(parameterName);
            }

            return value;
        }

        [ContractAnnotation("value:null => halt")]
        public static IReadOnlyList<T> NotEmpty<T>(IReadOnlyList<T> value,
            [InvokerParameterName] [JetBrains.Annotations.NotNull]
            string parameterName)
        {
            NotNull(value, parameterName);

            if (value.Count == 0)
            {
                NotEmpty(parameterName, nameof(parameterName));
#if NET10_0_OR_GREATER
                throw new ArgumentException(AbstractionsStrings.CollectionArgumentIsEmpty);
#else
                throw new ArgumentException(AbstractionsStrings.CollectionArgumentIsEmpty(parameterName));
#endif
            }

            return value;
        }

        [ContractAnnotation("value:null => halt")]
        public static string NotEmpty(string value,
            [InvokerParameterName] [JetBrains.Annotations.NotNull]
            string parameterName)
        {
            Exception e = null;
            if (value is null)
            {
                e = new ArgumentNullException(parameterName);
            }
            else if (value.Trim().Length == 0)
            {
#if NET10_0_OR_GREATER
                e = new ArgumentException(AbstractionsStrings.ArgumentIsEmpty);
#else
                e = new ArgumentException(AbstractionsStrings.ArgumentIsEmpty(parameterName));
#endif
            }

            if (e != null)
            {
                NotEmpty(parameterName, nameof(parameterName));

                throw e;
            }

            return value;
        }

        public static string NullButNotEmpty(string value,
            [InvokerParameterName] [JetBrains.Annotations.NotNull]
            string parameterName)
        {
            if (!(value is null)
                && value.Length == 0)
            {
                NotEmpty(parameterName, nameof(parameterName));
#if NET10_0_OR_GREATER
                throw new ArgumentException(AbstractionsStrings.ArgumentIsEmpty);
#else
                throw new ArgumentException(AbstractionsStrings.ArgumentIsEmpty(parameterName));
#endif
            }

            return value;
        }

        public static IReadOnlyList<T> HasNoNulls<T>(IReadOnlyList<T> value,
            [InvokerParameterName] [JetBrains.Annotations.NotNull]
            string parameterName)
            where T : class
        {
            NotNull(value, parameterName);

            if (value.Any(e => e == null))
            {
                NotEmpty(parameterName, nameof(parameterName));

                throw new ArgumentException(parameterName);
            }

            return value;
        }

        public static IReadOnlyList<string> HasNoEmptyElements(
            IReadOnlyList<string> value,
            [InvokerParameterName] [JetBrains.Annotations.NotNull]
            string parameterName)
        {
            NotNull(value, parameterName);

            if (value.Any(s => string.IsNullOrWhiteSpace(s)))
            {
                NotEmpty(parameterName, nameof(parameterName));

#if NET10_0_OR_GREATER
                throw new ArgumentException(AbstractionsStrings.CollectionArgumentHasEmptyElements);
#else
                throw new ArgumentException(AbstractionsStrings.CollectionArgumentHasEmptyElements(parameterName));
#endif


            }

            return value;
        }

        [Conditional("DEBUG")]
        public static void DebugAssert([DoesNotReturnIf(false)] bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Check.DebugAssert failed: {message}");
            }
        }
    }
}