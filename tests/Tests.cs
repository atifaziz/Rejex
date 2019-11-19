namespace RegexLinq.Tests
{
    using System;
    using System.Text.RegularExpressions;
    using NUnit.Framework;

    public class Tests
    {
        [Test]
        public void Binding()
        {
            var binding =
                from y in RegexBinding.Group("y")
                join m in RegexBinding.Group("m") on 1 equals 1
                join d in RegexBinding.Group("d") on 1 equals 1
                select new DateTime(int.Parse(y.Value), int.Parse(m.Value), int.Parse(d.Value));

            var match = Regex.Match("2012-12-21", @"^(?<y>[0-9]{4})-(?<m>[0-9]{2})-(?<d>[0-9]{2})$");

            var (matched, result) = match.Bind(binding);

            Assert.That(matched, Is.True);
            Assert.That(result, Is.EqualTo(new DateTime(2012, 12, 21)));
       }
    }
}
