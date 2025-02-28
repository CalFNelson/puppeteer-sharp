using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PuppeteerSharp.Tests.Attributes;
using PuppeteerSharp.Nunit;
using NUnit.Framework;

namespace PuppeteerSharp.Tests.NetworkTests
{
    public class ResponseJsonTests : PuppeteerPageBaseTest
    {
        public ResponseJsonTests(): base()
        {
        }

        [PuppeteerTest("network.spec.ts", "Response.json", "should work")]
        [Skip(SkipAttribute.Targets.Firefox)]
        public async Task ShouldWork()
        {
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/simple.json");
            Assert.AreEqual(JObject.Parse("{foo: 'bar'}"), await response.JsonAsync());
        }
    }
}
