using System.Collections.Generic;
using System.Linq;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils;
using BattleNetPrefill.Utils.Debug;
using BattleNetPrefill.Utils.Debug.Models;
using NUnit.Framework;

namespace BattleNetPrefill.Test.DebugUtilTests
{
    [TestFixture]
    public class RequestUtilsTests
    {
        [Test]
        public void DuplicateRequests_GetCombined()
        {
            var requests = new List<Request>
            {
                // Creating two requests that are exact duplicates
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100, DownloadWholeFile = true },
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100, DownloadWholeFile = true }
            };

            var result = RequestUtils.CoalesceRequests(requests);

            // Expect there to be 1 result left, since we deduped the final request.
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void DuplicatesCreatedFromCombinedRequests_WillGetRemoved()
        {
            var requests = new List<Request>
            {
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100, DownloadWholeFile = true },
                // Creating two requests that will be combined into a single request, that is a duplicate of the above
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 0, UpperByteRange = 49, DownloadWholeFile = true },
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 50, UpperByteRange = 100, DownloadWholeFile = true }
            };

            var result = RequestUtils.CoalesceRequests(requests);

            // Expect there to be 1 result left, since we deduped the final request.
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void RequestsThatDontMatch_OnUri_WontGetCombined()
        {
            var requests = new List<Request>
            {
                // Requests differ by ProductRootUri, won't be combined
                new Request { ProductRootUri = "SampleUri", RootFolder = RootFolder.data, CdnKey = "01d2060c711a275d39be37732fbeee16".ToMD5(),
                                    LowerByteRange = 0, UpperByteRange = 100, DownloadWholeFile = true },
                new Request { ProductRootUri = "DifferentURI", RootFolder = RootFolder.data, CdnKey = "0002060c711a275d39be37732fbeee16".ToMD5(), 
                                    LowerByteRange = 0, UpperByteRange = 100, DownloadWholeFile = true }
            };

            var result = RequestUtils.CoalesceRequests(requests);

            // Expect 2 results, since they wont get combined
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void RequestsThatDontMatch_OnLowerByteRange_WontGetCombined()
        {
            var requests = new List<Request>
            {
                // Requests differ by LowerByteRange, won't be combined
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100 },
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 9999, UpperByteRange = 100 }
            };

            var result = RequestUtils.CoalesceRequests(requests);

            // Expect 2 results, since they wont get combined
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void RequestsThatOverlap_OnUpperByteRange_GetCombined()
        {
            var requests = new List<Request>
            {
                // Overlap on the upper byte range
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100 },
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 0, UpperByteRange = 999999 }
            };

            var result = RequestUtils.CoalesceRequests(requests);

            // Expect 1 results, since they got combined
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(0, result[0].LowerByteRange);
            Assert.AreEqual(999999, result[0].UpperByteRange);
        }
        
        [Test]
        public void SequentialByteRanges_WillBeCombined()
        {
            var requests = new List<Request>
            {
                // These two requests have sequential byte ranges (0-100 -> 101-200), so they should be combined
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100 },
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 101, UpperByteRange = 200 }
            };

            var results = RequestUtils.CoalesceRequests(requests);

            // Expect 1 result, with the byte range being the combination of the two
            Assert.AreEqual(1, results.Count);

            var combinedResult = results.FirstOrDefault();
            Assert.AreEqual(0, combinedResult.LowerByteRange);
            Assert.AreEqual(200, combinedResult.UpperByteRange);
        }

        [Test]
        public void SequentialByteRanges_WillBeCombined_ReversedOrder()
        {
            var requests = new List<Request>
            {
                // These two requests have sequential byte ranges (0-100 -> 101-200), so they should be combined
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 101, UpperByteRange = 200 },
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100 }
            };

            var results = RequestUtils.CoalesceRequests(requests);

            // Expect 1 result, with the byte range being the combination of the two
            Assert.AreEqual(1, results.Count);

            var combinedResult = results.FirstOrDefault();
            Assert.AreEqual(0, combinedResult.LowerByteRange);
            Assert.AreEqual(200, combinedResult.UpperByteRange);
        }

        [Test]
        public void SequentialByteRanges_DifferentUri_WontBeCombined()
        {
            var requests = new List<Request>
            {
                // These two requests have sequential byte ranges, however they are not to the same request so they will not be combined
                new Request { ProductRootUri = "SampleUri", RootFolder = RootFolder.data, CdnKey = "01d2060c711a275d39be37732fbeee16".ToMD5(), LowerByteRange = 0, UpperByteRange = 100 },
                new Request { ProductRootUri = "DifferentUri", RootFolder = RootFolder.data, CdnKey = "1112060c711a275d39be37732fbeee16".ToMD5(), LowerByteRange = 101, UpperByteRange = 200 }
            };

            var results = RequestUtils.CoalesceRequests(requests);

            // Expect 2 results, since they wont get combined
            Assert.AreEqual(2, results.Count);

            // Validating that the requests are untouched
            var firstRequest = results.Single(e => e.Uri.Contains("SampleUri"));
            Assert.AreEqual(0, firstRequest.LowerByteRange);
            Assert.AreEqual(100, firstRequest.UpperByteRange);

            var secondRequest = results.Single(e => e.Uri.Contains("DifferentUri"));
            Assert.AreEqual(101, secondRequest.LowerByteRange);
            Assert.AreEqual(200, secondRequest.UpperByteRange);
        }

        [Test]
        public void OverlappingByteRanges_WillBeCombined()
        {
            var requests = new List<Request>
            {
                // These two requests have byte ranges that overlap, so they should be combined into a single entry
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 0, UpperByteRange = 50 },
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 25, UpperByteRange = 100 }
            };

            var results = RequestUtils.CoalesceRequests(requests);

            // Expect 1 result, with the byte range being the combination of the two
            Assert.AreEqual(1, results.Count);

            var combinedResult = results.FirstOrDefault();
            Assert.AreEqual(0, combinedResult.LowerByteRange);
            Assert.AreEqual(100, combinedResult.UpperByteRange);
        }

        [Test]
        public void OverlappingByteRanges_WillBeCombined_Reversed()
        {
            var requests = new List<Request>
            {
                // These two requests have byte ranges that overlap, so they should be combined into a single entry
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 260046848, UpperByteRange = 268435455 },
                new Request { ProductRootUri = "SampleUri", LowerByteRange = 264241152, UpperByteRange = 269484031 }
            };

            var results = RequestUtils.CoalesceRequests(requests);

            // Expect 1 result, with the byte range being the combination of the two
            Assert.AreEqual(1, results.Count);

            var combinedResult = results.FirstOrDefault();
            Assert.AreEqual(260046848, combinedResult.LowerByteRange);
            Assert.AreEqual(269484031, combinedResult.UpperByteRange);
        }
    }
}
