﻿// ReSharper disable RedundantUsingDirective

using System;
using System.Linq;

using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Providers.Core;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common.AutoMoq;

namespace NzbDrone.Core.Test.ProviderTests
{
    [TestFixture]
    // ReSharper disable InconsistentNaming
    public class RootDirProviderTest : CoreTest
    {

        [Test]
        public void GetRootDirs()
        {
            //Setup
            

            var emptyDatabase = TestDbHelper.GetEmptyDatabase();
            Mocker.SetConstant(emptyDatabase);
            emptyDatabase.Insert(new RootDir { Path = @"C:\TV" });
            emptyDatabase.Insert(new RootDir { Path = @"C:\TV2" });

            //Mocker.GetMock<IRepository>()
            //    .Setup(f => f.All<RootDir>())
            //    .Returns(sonicRepo.All<RootDir>);

            //Act
            var result = Mocker.Resolve<RootDirProvider>().GetAll();

            //Assert
            Assert.AreEqual(result.Count, 2);
        }

        [TestCase("D:\\TV Shows\\")]
        [TestCase("//server//folder")]
        public void AddRootDir(string path)
        {
            //Setup
            
            Mocker.SetConstant(TestDbHelper.GetEmptyDatabase());

            //Act
            var rootDirProvider = Mocker.Resolve<RootDirProvider>();
            rootDirProvider.Add(new RootDir { Path = path });


            //Assert
            var rootDirs = rootDirProvider.GetAll();
            rootDirs.Should().NotBeEmpty();
            rootDirs.Should().HaveCount(1);
            path.Should().Be(rootDirs.First().Path);
        }


        [Test]
        public void RemoveRootDir()
        {
            //Setup
            
            Mocker.SetConstant(TestDbHelper.GetEmptyDatabase());

            //Act
            var rootDirProvider = Mocker.Resolve<RootDirProvider>();
            rootDirProvider.Add(new RootDir { Path = @"C:\TV" });
            rootDirProvider.Remove(1);

            //Assert
            var rootDirs = rootDirProvider.GetAll();
            rootDirs.Should().BeEmpty();
        }

        [Test]
        public void GetRootDir()
        {
            //Setup
            
            Mocker.SetConstant(TestDbHelper.GetEmptyDatabase());

            const int id = 1;
            const string path = @"C:\TV";

            //Act
            var rootDirProvider = Mocker.Resolve<RootDirProvider>();
            rootDirProvider.Add(new RootDir { Id = id, Path = path });

            //Assert
            var rootDir = rootDirProvider.GetRootDir(id);
            rootDir.Id.Should().Be(1);
            rootDir.Path.Should().Be(path);
        }

        [Test]
        public void None_existing_folder_returns_empty_list()
        {
            const string path = "d:\\bad folder";

            
            Mocker.GetMock<DiskProvider>(MockBehavior.Strict)
                .Setup(m => m.FolderExists(path)).Returns(false);

            var result = Mocker.Resolve<RootDirProvider>().GetUnmappedFolders(path);

            result.Should().NotBeNull();
            result.Should().BeEmpty();

            Mocker.VerifyAllMocks();
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void empty_folder_path_throws()
        {
            
            Mocker.Resolve<RootDirProvider>().GetUnmappedFolders("");
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("BAD PATH")]
        [ExpectedException(typeof(ArgumentException))]
        public void invalid_folder_path_throws_on_add(string path)
        {
            
            Mocker.Resolve<RootDirProvider>().Add(new RootDir { Id = 0, Path = path });
        }

    }
}