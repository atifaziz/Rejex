#region Copyright (c) 2019 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace RegexLinq
{
    using System;
    using System.Text.RegularExpressions;
    using static OptionModule;

    static class OptionModule
    {
        public const bool SomeT = true;
        public static (bool, T) Some<T>(T value) => (SomeT, value);
    }

    partial interface IRegexBinding<T>
    {
        (bool Success, T Value) Bind(Match match);
    }

    static partial class RegexBinding
    {
        public static (bool, T) Match<T>(string input, string pattern, IRegexBinding<T> binding) =>
            binding.Bind(Regex.Match(input, pattern));
    }

    static partial class RegexBinding
    {
        public static (bool, T) Bind<T>(this Match match, IRegexBinding<T> binder) =>
            binder.Bind(match);

        public static IRegexBinding<T> Create<T>(Func<Match, (bool, T)> binder) =>
            new DelegatingIRegexBinding<T>(binder);

        public static IRegexBinding<T> Return<T>(T value) => Create(_ => Some(value));

        public static IRegexBinding<Group> Group(string name) => Create(m => m.Groups[name] is Group g && g.Success ? Some(g) : default);
        public static IRegexBinding<Group> Group(int num) => Create(m => m.Groups[num] is Group g && g.Success ? Some(g) : default);

        public static IRegexBinding<U> Map<T, U>(this IRegexBinding<T> binding, Func<T, U> f) =>
            Create(m => binding.Bind(m) is (SomeT, var v) ? Some(f(v)) : default);

        public static IRegexBinding<U> Bind<T, U>(this IRegexBinding<T> binding, Func<T, IRegexBinding<U>> f) =>
            Create(m => binding.Bind(m) is (SomeT, var t) && f(t).Bind(m) is (SomeT, var u) ? Some(u) : default);

        public static IRegexBinding<U> Select<T, U>(this IRegexBinding<T> binding, Func<T, U> f) =>
            binding.Map(f);

        public static IRegexBinding<U> SelectMany<T, U>(this IRegexBinding<T> binding, Func<T, IRegexBinding<U>> f) =>
            binding.Bind(f);

        public static IRegexBinding<V> SelectMany<T, U, V>(this IRegexBinding<T> binding, Func<T, IRegexBinding<U>> f, Func<T, U, V> g) =>
            binding.Bind(t => f(t).Bind(u => Return(g(t, u))));

        public static IRegexBinding<(T, U)>
            Zip<T, U>(this IRegexBinding<T> first,
                      IRegexBinding<U> second) =>
            Create(m => first.Bind(m) is (SomeT, var a) && second.Bind(m) is (SomeT, var b) ? Some((a, b)) : default);

        public static IRegexBinding<V>
            Join<T, U, K, V>(this IRegexBinding<T> first,
                             IRegexBinding<U> second,
                             Func<T, K> kf1, Func<U, K> kf2,
                             Func<T, U, V> f) =>
            from e in first.Zip(second)
            select f(e.Item1, e.Item2);

        sealed class DelegatingIRegexBinding<T> : IRegexBinding<T>
        {
            readonly Func<Match, (bool, T)> _binder;
            public DelegatingIRegexBinding(Func<Match, (bool, T)> binder) => _binder = binder;
            public (bool Success, T Value) Bind(Match match) => _binder(match);
        }
    }
}
