using FlowSynx.Application.PluginHost.Parser;

namespace FlowSynx.Application.UnitTests.PluginHost.Parser
{
    public sealed class ParsedPluginTypeTests
    {
        [Fact]
        public void Ctor_WithTypeAndCurrentVersion_SetsProperties_And_IsNotUpdate()
        {
            var sut = new ParsedPluginType("pluginA", "1.0.0");

            Assert.Equal("pluginA", sut.Type);
            Assert.Equal("1.0.0", sut.CurrentVersion);
            Assert.Null(sut.TargetVersion);
            Assert.False(sut.IsUpdate);
        }

        [Fact]
        public void Ctor_WithAllArgs_SetsProperties_And_IsUpdate()
        {
            var sut = new ParsedPluginType("pluginA", "1.0.0", "2.0.0");

            Assert.Equal("pluginA", sut.Type);
            Assert.Equal("1.0.0", sut.CurrentVersion);
            Assert.Equal("2.0.0", sut.TargetVersion);
            Assert.True(sut.IsUpdate);
        }

        [Fact]
        public void Ctor_NullType_Throws()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new ParsedPluginType(null!, "1.0.0"));
            Assert.Equal("type", ex.ParamName);
        }

        [Fact]
        public void Ctor_NullCurrentVersion_Throws()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new ParsedPluginType("pluginA", null!));
            Assert.Equal("currentVersion", ex.ParamName);
        }

        [Theory]
        [InlineData("latest", null, false, true)] // current=latest, no update => latest
        [InlineData("latest", "2.0.0", true, false)] // update overrides current latest
        [InlineData("1.0.0", "latest", true, true)] // target=latest => latest
        [InlineData("1.0.0", null, false, false)]
        [InlineData("LATEST", null, false, false)] // case-sensitive check
        public void IsLatest_Combinations(string current, string? target, bool expectedIsUpdate, bool expectedIsLatest)
        {
            var sut = new ParsedPluginType("pluginA", current, target);

            Assert.Equal(expectedIsUpdate, sut.IsUpdate);
            Assert.Equal(expectedIsLatest, sut.IsLatest);
        }

        [Theory]
        [InlineData("pluginA", "1.0.0", null, "pluginA:1.0.0")]
        [InlineData("pluginA", "1.0.0", "2.0.0", "pluginA:1.0.0->2.0.0")]
        [InlineData("pluginA", "latest", null, "pluginA:latest")]
        [InlineData("pluginA", "latest", "latest", "pluginA:latest->latest")]
        public void ToString_Formats_AsExpected(string type, string current, string? target, string expected)
        {
            var sut = new ParsedPluginType(type, current, target);

            var text = sut.ToString();

            Assert.Equal(expected, text);
        }

        [Fact]
        public void ToString_WhenUpdate_UsesArrowFormat()
        {
            var sut = new ParsedPluginType("pluginB", "3.1.4", "4.0.0");

            Assert.True(sut.IsUpdate);
            Assert.Equal("pluginB:3.1.4->4.0.0", sut.ToString());
        }

        [Fact]
        public void ToString_WhenNotUpdate_UsesTypeColonCurrent()
        {
            var sut = new ParsedPluginType("pluginC", "0.9.0");

            Assert.False(sut.IsUpdate);
            Assert.Equal("pluginC:0.9.0", sut.ToString());
        }

        [Fact]
        public void IsUpdate_WhenTargetVersionIsNull_IsFalse()
        {
            var sut = new ParsedPluginType("pluginD", "1.2.3");

            Assert.False(sut.IsUpdate);
        }

        [Fact]
        public void IsUpdate_WhenTargetVersionIsSet_IsTrue()
        {
            var sut = new ParsedPluginType("pluginD", "1.2.3", "2.0.0");

            Assert.True(sut.IsUpdate);
        }
    }
}