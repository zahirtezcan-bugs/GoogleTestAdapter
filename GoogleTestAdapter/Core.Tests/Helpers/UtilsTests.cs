﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class UtilsTests
    {
        [TestMethod]
        [TestCategory(Unit)]
        public void DeleteDirectory_CanNotBeDeleted_ReturnsFalseAndMessage()
        {
            string dir = Utils.GetTempDirectory();
            SetReadonlyFlag(dir);

            string errorMessage;
            bool result = Utils.DeleteDirectory(dir, out errorMessage);

            result.Should().BeFalse();
            errorMessage.Should().Contain(dir);

            RemoveReadonlyFlag(dir);

            result = Utils.DeleteDirectory(dir, out errorMessage);

            result.Should().BeTrue();
            errorMessage.Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTempDirectory__DirectoryDoesExistAndCanBeDeleted()
        {
            string dir = Utils.GetTempDirectory();
            Directory.Exists(dir).Should().BeTrue();

            // ReSharper disable once UnusedVariable
            Utils.DeleteDirectory(dir, out string errorMessage).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void BinaryFileContainsStrings_TestX86Release_ShouldContainGoogleTestIndicator()
        {
            Utils.BinaryFileContainsStrings(TestResources.Tests_ReleaseX86, Encoding.ASCII, GoogleTestConstants.GoogleTestExecutableMarkers).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void BinaryFileContainsStrings_TestX64Release_ShouldContainGoogleTestIndicator()
        {
            Utils.BinaryFileContainsStrings(TestResources.Tests_ReleaseX64, Encoding.ASCII, GoogleTestConstants.GoogleTestExecutableMarkers).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void BinaryFileContainsStrings_TestX86Debug_ShouldContainGoogleTestIndicator()
        {
            Utils.BinaryFileContainsStrings(TestResources.Tests_DebugX86, Encoding.ASCII, GoogleTestConstants.GoogleTestExecutableMarkers).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void BinaryFileContainsStrings_TestX64Debug_ShouldContainGoogleTestIndicator()
        {
            Utils.BinaryFileContainsStrings(TestResources.Tests_DebugX64, Encoding.ASCII, GoogleTestConstants.GoogleTestExecutableMarkers).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void BinaryFileContainsStrings_TenSecondsWaiter_ShouldNotContainGoogleTestIndicator()
        {
            Utils.BinaryFileContainsStrings(TestResources.TenSecondsWaiter, Encoding.ASCII, GoogleTestConstants.GoogleTestExecutableMarkers).Should().BeFalse();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void BinaryFileContainsStrings_EmptyFile_ShouldNotContainGoogleTestIndicator()
        {
            Utils.BinaryFileContainsStrings(TestResources.TenSecondsWaiter, Encoding.ASCII, GoogleTestConstants.GoogleTestExecutableMarkers).Should().BeFalse();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void TimestampMessage_MessageIsNullOrEmpty_ResultIsTheSame()
        {
            string timestampSeparator = " - ";
            string resultRegex = @"[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}" + timestampSeparator;

            string nullMessage = null;
            Utils.TimestampMessage(ref nullMessage);
            nullMessage.Should().MatchRegex(resultRegex);
            nullMessage.Should().EndWith(timestampSeparator);

            string emptyMessage = "";
            Utils.TimestampMessage(ref emptyMessage);
            emptyMessage.Should().MatchRegex(resultRegex);
            emptyMessage.Should().EndWith(timestampSeparator);

            string fooMessage = "foo";
            Utils.TimestampMessage(ref fooMessage);
            fooMessage.Should().MatchRegex(resultRegex);
            fooMessage.Should().EndWith(timestampSeparator + "foo");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void SpawnAndWait_TwoTasks_AreExecutedInParallel()
        {
            int nrOfTasks = Environment.ProcessorCount;
            if (nrOfTasks < 2)
                Assert.Inconclusive("System only has one processor, skipping test");

            int taskDurationInMs = 500;

            var tasks = new Action[nrOfTasks];
            for (int i = 0; i < nrOfTasks; i++)
            {
                tasks[i] = () => Thread.Sleep(taskDurationInMs);
            }

            var stopWatch = Stopwatch.StartNew();
            Utils.SpawnAndWait(tasks);
            stopWatch.Stop();

            stopWatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(taskDurationInMs);
            stopWatch.ElapsedMilliseconds.Should().BeLessThan(2 * taskDurationInMs);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void SpawnAndWait_TaskWithTimeout_TimeoutsAndReturnsFalse()
        {
            int taskDurationInMs = 500, timeoutInMs = taskDurationInMs / 2;
            var tasks = new Action[] { () => Thread.Sleep(taskDurationInMs) };

            var stopWatch = Stopwatch.StartNew();
            bool hasFinishedTasks = Utils.SpawnAndWait(tasks, timeoutInMs);
            stopWatch.Stop();

            hasFinishedTasks.Should().BeFalse();
            stopWatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(timeoutInMs - TestMetadata.ToleranceInMs);
            stopWatch.ElapsedMilliseconds.Should().BeLessThan(taskDurationInMs);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ValidatePattern_EmptyPattern_BothPartsReported()
        {
            bool result = Utils.ValidatePattern("", out string errorMessage);

            result.Should().BeFalse();
            errorMessage.Should().Contain("file pattern part");
            errorMessage.Should().Contain("path part");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ValidatePattern_TempDir_FilePartReported()
        {
            bool result = Utils.ValidatePattern(Path.GetTempPath(), out string errorMessage);

            result.Should().BeFalse();
            errorMessage.Should().Contain("file pattern part");
            errorMessage.Should().NotContain("path part");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ValidatePattern_LocalFile_PathPartReported()
        {
            bool result = Utils.ValidatePattern(@"InvalidPath::\Foo.exe", out string errorMessage);

            result.Should().BeFalse();
            errorMessage.Should().NotContain("file pattern part");
            errorMessage.Should().Contain("path part");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ValidatePattern_ValidInput_ValidationSuceeds()
        {
            bool result = Utils.ValidatePattern(@"C:\foo\Bar.exe", out string errorMessage);

            result.Should().BeTrue();
            errorMessage.Should().BeNullOrEmpty();
        }

        private void SetReadonlyFlag(string dir)
        {
            FileAttributes fileAttributes = File.GetAttributes(dir);
            fileAttributes |= FileAttributes.ReadOnly;
            File.SetAttributes(dir, fileAttributes);
        }

        private void RemoveReadonlyFlag(string dir)
        {
            FileAttributes fileAttributes = File.GetAttributes(dir);
            fileAttributes = fileAttributes & ~FileAttributes.ReadOnly;
            File.SetAttributes(dir, fileAttributes);
        }

    }

}