﻿using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Htc.Vita.Core.Tests
{
    public class UriSchemeManager
    {
        private readonly ITestOutputHelper _output;

        public UriSchemeManager(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Default_0_GetSystemUriScheme()
        {
            if (!Runtime.Platform.IsWindows)
            {
                return;
            }

            var uriSchemeManager = Shell.UriSchemeManager.GetInstance();
            Assert.NotNull(uriSchemeManager);

            var systemUriScheme = uriSchemeManager.GetSystemUriScheme("http");
            Assert.NotNull(systemUriScheme);
            _output.WriteLine("systemUriScheme.Name: \"" + systemUriScheme.Name + "\"");
            Assert.False(string.IsNullOrEmpty(systemUriScheme.Name));
            _output.WriteLine("systemUriScheme.DefaultIcon: \"" + systemUriScheme.DefaultIcon + "\"");
            Assert.False(string.IsNullOrEmpty(systemUriScheme.DefaultIcon));
            _output.WriteLine("systemUriScheme.CommandPath: \"" + systemUriScheme.CommandPath + "\"");
            Assert.False(string.IsNullOrEmpty(systemUriScheme.CommandPath));
            _output.WriteLine("systemUriScheme.CommandParameter: \"" + systemUriScheme.CommandParameter + "\"");
            Assert.False(string.IsNullOrEmpty(systemUriScheme.CommandParameter));

            var systemUriScheme2 = uriSchemeManager.GetSystemUriScheme("http2");
            Assert.NotNull(systemUriScheme2);
            Assert.False(string.IsNullOrEmpty(systemUriScheme2.Name));
            Assert.True(string.IsNullOrEmpty(systemUriScheme2.DefaultIcon));
            Assert.True(string.IsNullOrEmpty(systemUriScheme2.CommandPath));
            Assert.True(string.IsNullOrEmpty(systemUriScheme2.CommandParameter));
        }

        [Fact]
        public void Default_0_GetSystemUriScheme_WithWhitelist()
        {
            if (!Runtime.Platform.IsWindows)
            {
                return;
            }

            var uriSchemeManager = Shell.UriSchemeManager.GetInstance();
            Assert.NotNull(uriSchemeManager);

            var options = new Dictionary<string, string>
            {
                {Shell.UriSchemeManager.OptionAcceptWhitelistOnly, "true"}
            };
            var systemUriScheme = uriSchemeManager.GetSystemUriScheme("http", options);
            Assert.NotNull(systemUriScheme);
            _output.WriteLine("systemUriScheme.Name: \"" + systemUriScheme.Name + "\"");
            _output.WriteLine("systemUriScheme.DefaultIcon: \"" + systemUriScheme.DefaultIcon + "\"");
            _output.WriteLine("systemUriScheme.CommandPath: \"" + systemUriScheme.CommandPath + "\"");
            _output.WriteLine("systemUriScheme.CommandParameter: \"" + systemUriScheme.CommandParameter + "\"");

            var systemUriScheme2 = uriSchemeManager.GetSystemUriScheme("http2", options);
            Assert.NotNull(systemUriScheme2);
            Assert.False(string.IsNullOrEmpty(systemUriScheme2.Name));
            Assert.True(string.IsNullOrEmpty(systemUriScheme2.DefaultIcon));
            Assert.True(string.IsNullOrEmpty(systemUriScheme2.CommandPath));
            Assert.True(string.IsNullOrEmpty(systemUriScheme2.CommandParameter));
        }
    }
}
