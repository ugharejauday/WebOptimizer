﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders.Physical;
using Moq;
using Xunit;

namespace WebOptimizer.Test.Processors
{
    public class CssFinterprinterTest
    {
        [Theory2]
        [InlineData("url(/img/foo.png)", "url(/img/foo.png?v=Ai9EHcgOXDloih8M5cRTS07P-FI)")]
        [InlineData("url(/img/foo.png?1=1)", "url(/img/foo.png?v=Ai9EHcgOXDloih8M5cRTS07P-FI&1=1)")]
        [InlineData("url('/img/foo.png')", "url('/img/foo.png?v=Ai9EHcgOXDloih8M5cRTS07P-FI')")]
        [InlineData("url('/img/doesntexist.png')", "url('/img/doesntexist.png')")]
        [InlineData("url(http://foo.png)", "url(http://foo.png)")]
        public async Task CssFingerprint_Success(string url, string newUrl)
        {
            var adjuster = new CssFingerprinter();
            var context = new Mock<IAssetContext>().SetupAllProperties();
            var pipeline = new Mock<IAssetPipeline>().SetupAllProperties();

            string temp = Path.GetTempPath();
            string path = Path.Combine(temp, "css", "img");
            Directory.CreateDirectory(path);
            string imagePath = Path.Combine(path, "foo.png");
            File.WriteAllText(imagePath, string.Empty);
            File.SetLastWriteTime(imagePath, new DateTime(2017, 1, 1));

            var inputFile = new PhysicalFileInfo(new FileInfo(Path.Combine(temp, "css", "site.css")));
            var outputFile = new PhysicalFileInfo(new FileInfo(Path.Combine(temp, "dist", "all.css")));

            context.SetupGet(s => s.Asset.Route)
                   .Returns("/my/route.css");

            context.Setup(s => s.HttpContext.RequestServices.GetService(typeof(IAssetPipeline)))
                   .Returns(pipeline.Object);

            pipeline.SetupSequence(s => s.FileProvider.GetFileInfo(It.IsAny<string>()))
                   .Returns(inputFile)
                   .Returns(outputFile);

            context.Object.Content = new Dictionary<string, string> { { "css/site.css", url } };

            await adjuster.ExecuteAsync(context.Object);
            string result = context.Object.Content.First().Value;

            Assert.Equal(newUrl, result);
            Assert.Equal("", adjuster.CacheKey(new DefaultHttpContext()));
        }
    }
}