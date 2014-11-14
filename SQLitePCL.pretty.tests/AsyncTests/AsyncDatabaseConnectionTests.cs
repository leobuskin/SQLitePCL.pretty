﻿/*
   Copyright 2014 David Bordoley

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

using SQLitePCL.pretty;

using NUnit.Framework;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public class AsyncDatabaseConnectionTests
    {
        [Test]
        public async Task TestUse()
        {
            using (var db = SQLite3.Open(":memory:").AsAsyncDatabaseConnection(NewThreadScheduler.Default))
            {
                Console.WriteLine("root thread:" + Thread.CurrentThread.ManagedThreadId);
                await db.Use(conn =>
                    {
                        Console.WriteLine("excute thread:" + Thread.CurrentThread.ManagedThreadId);
                        conn.ExecuteAll(
                            @"CREATE TABLE foo (x int);
                              INSERT INTO foo (x) VALUES (1);
                              INSERT INTO foo (x) VALUES (2);
                              INSERT INTO foo (x) VALUES (3);");
                    });

                
                //db.Query("SELECT * FROM foo;", result =>
                 //       {
                  //          Console.WriteLine("result thread" + Thread.CurrentThread.ManagedThreadId + " " + result[0].ToInt());
                   //         return result[0].ToInt();
                    //    })
                   // .ObserveOn(NewThreadScheduler.Default);
                //.Do(r =>
                //    {
                //       Console.WriteLine("do thread" + Thread.CurrentThread.ManagedThreadId);
                //      Console.WriteLine(r);
                //  });

                var x = db.Query("SELECT * FROM foo;", result =>
                {
                    Console.WriteLine("x result thread" + Thread.CurrentThread.ManagedThreadId + " " + result[0].ToInt());
                    return result[0].ToInt();
                })
    .ObserveOn(NewThreadScheduler.Default);

                var y = db.Query("SELECT * FROM foo;", result =>
                    {
                        Console.WriteLine("y result thread" + Thread.CurrentThread.ManagedThreadId + " " + result[0].ToInt());
                        return result[0].ToInt();
                    })
                    .ObserveOn(NewThreadScheduler.Default);
                //.Do(r =>
                //{
                //    Console.WriteLine("y do thread" + Thread.CurrentThread.ManagedThreadId);
                //   Console.WriteLine(r);
                //});

                await Task.WhenAll(y.ToTask(), x.ToTask(), y.ToTask(), x.ToTask(), y.ToTask());
            }
        }
    }
}
