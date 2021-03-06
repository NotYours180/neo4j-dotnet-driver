﻿// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.Routing
{
    public class RoundRobinRoutingTableTests
    {
        private static IEnumerable<Uri> CreateUriArray(int count)
        {
            var uris = new Uri[count];
            for (int i = 0; i < count; i++)
            {
                uris[i] = new Uri($"http://neo4j:1{i}");
            }
            return uris;
        }

        public class IsStatleMethod
        {
            [Theory] [InlineData(1, 2, 1, 5 * 60, false)] // 1 router, 2 reader, 1 writer
            [InlineData(0, 2, 1, 5 * 60, true)] // no router
            [InlineData(2, 2, 0, 5 * 60, false)] // no writer
            [InlineData(2, 0, 2, 5 * 60, true)] // no reader
            [InlineData(1, 2, 1, -1, true)] // expire immediately
            public void ShouldBeStaleInReadModeIfOnlyHaveOneRouter(int routerCount, int readerCount, int writerCount, long expireAfterSeconds, bool isStale)
            {
                var table = new RoundRobinRoutingTable(
                    CreateUriArray(routerCount),
                    CreateUriArray(readerCount),
                    CreateUriArray(writerCount),
                    new Stopwatch(),
                    expireAfterSeconds);
                table.IsStale(AccessMode.Read).Should().Be(isStale);
            }

            [Theory]
            [InlineData(1, 2, 1, 5 * 60, false)] // 1 router, 2 reader, 1 writer
            [InlineData(0, 2, 1, 5 * 60, true)] // no router
            [InlineData(2, 2, 0, 5 * 60, true)] // no writer
            [InlineData(2, 0, 2, 5 * 60, false)] // no reader
            [InlineData(1, 2, 1, -1, true)] // expire immediately
            public void ShouldBeStaleInWriteModeIfOnlyHaveOneRouter(int routerCount, int readerCount, int writerCount, long expireAfterSeconds, bool isStale)
            {
                var table = new RoundRobinRoutingTable(
                    CreateUriArray(routerCount),
                    CreateUriArray(readerCount),
                    CreateUriArray(writerCount),
                    new Stopwatch(),
                    expireAfterSeconds);
                table.IsStale(AccessMode.Write).Should().Be(isStale);
            }
        }

        public class PrependRouterMethod
        {
            [Fact]
            public void ShouldInjectInFront()
            {
                // Given
                var table = new RoundRobinRoutingTable(
                    CreateUriArray(3),
                    CreateUriArray(0),
                    CreateUriArray(0),
                    new Stopwatch(), 5 * 60);
                Uri router;
                table.TryNextRouter(out router);
                var head = new Uri("http://neo4j:10");
                router.Should().Be(head);

                // When
                var first = new Uri("me://12");
                var second = new Uri("me://22");
                table.PrependRouters(new List<Uri> {first, second});

                // Then
                table.TryNextRouter(out router);
                router.Should().Be(first);
                table.TryNextRouter(out router);
                router.Should().Be(second);
                table.TryNextRouter(out router);
                router.Should().Be(head);
            }
        }

    }
}
