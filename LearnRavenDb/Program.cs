namespace LearnRavenDb
{
    using System;
    using Raven.Client.Documents;
    using Orders;
    using System.Collections.Generic;
    using System.Linq;
    using Raven.Client.Documents.Indexes;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World! I'm trying to learn RavenDB .NET with C#!");
            Console.WriteLine("---------------------------------------------------------------");

            // --------------------- Document Store Initialization --------------------- //
            Console.WriteLine();
            Console.WriteLine("DocumentStore has been created as a Singleton instance.");
            Console.WriteLine("Refer to DocumentStoreSingleton.cs (Located at [Solution Folder]/LearnRavenDb/DocumentSingleton.cs");
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
            // --------------------- END Document Store Initialization

            /*
             * DOCKER notes
             * Use the following Docker command to setup a docker container
             *  RavenDb is ran on an Ubuntu 16.04 container.
             * docker run -d -p 8080:8080 -p 38888:388888 -e UNSECURED_ACCESS_ALLOWED=PublicNetwork -e PUBLIC_SERVER_URL=http://localhost:8080 ravendb/ravendb:latest
             */

            /*
             *  Connection String
             * Since we are using the OpenSource version of RavenDb, we do not have access to the various security policies it uses.
             * A typical connection string with open access follows the following format
             * Url=http://localhost:8080;Database=Northwind
             */

            // --------------------- Load Document from the Server --------------------- //
            using (var session = DocumentStoreSingleton.Store.OpenSession())
            {
                Console.WriteLine("Attempting to assign record 'products/1' to objects P and P2.");
                var p = session.Load<Product>("products/1");
                Console.WriteLine("P Name: " + p.Name);
                // --------------------- END Load Document from the Server

                // --------------------- Comparing Objects --------------------- //
                /*
                 * The p2 load call never actually makes it to the server. The RavenDb client pulls that information from the cache.
                 * */
                var p2 = session.Load<Product>("products/1");
                Console.WriteLine("P2 Name: " + p2.Name);

                if (p == p2) { Console.WriteLine("Object P and P2 are equal!"); }
                Console.WriteLine("---------------------------------------------------------------");
                // --------------------- END Comparing Objects

                // --------------------- Calling Multiple Records at a Single Time --------------------- //
                Console.WriteLine("Attempting to retrieve record 'products/3' by supplying '3' as a paramter");
                Console.WriteLine("Example: session.Load<Product>(3)");
                Console.WriteLine("Not Available in RavenDb 4 RC. :(");
                Console.WriteLine("Resorted to using 'products/3");
                Product p3 = session.Load<Product>("products/3");
                Console.WriteLine("P3 Name: " + p3.Name);
                Console.WriteLine("------------------------------------------------------------------------------------");

                Console.WriteLine("Passing various Product IDs to a list to make a single call to the RavenDb server.");
                IEnumerable<string> productIdList = new List<string>
                {
                    "products/4",
                    "products/5",
                    "products/6"
                };

                var products = session.Load<Product>(productIdList); // Returns a dictionary value.

                foreach(var prod in products)
                {
                    Console.WriteLine(prod.Value.Name);
                }
                Console.WriteLine("------------------------------------------------------------------------------------");
                // --------------------- END Calling Multiple Records at a Single Time
            }


            using (var session = DocumentStoreSingleton.Store.OpenSession())
            {
                // --------------------- Loading Related Documents --------------------- //
                // Remember to 'Include' all record types in query.
                var order = session
                            .Include<Order>(x => x.Company)
                            .Include(x => x.Employee)
                            .Include(x => x.Lines.Select(l => l.Product))
                            .Load("orders/1");

                // We still have to 'query' the other types. No remote call is issued to the RavenDb server.
                // The client stores the 'included' types in the session cache.
                Company company = session.Load<Company>(order.Company);
                Employee employee = session.Load<Employee>(order.Employee);

                // Retrieve string id of product.
                List<string> lineProductIds = new List<string>();
                foreach(OrderLine orderLine in order.Lines)
                {
                    lineProductIds.Add(orderLine.Product);
                }

                var lineProducts = session.Load<Product>(lineProductIds);

                Console.WriteLine("Order Information");
                Console.WriteLine(order.Id);
                Console.WriteLine(order.Company);
                Console.WriteLine(order.Employee);
                foreach(OrderLine line in order.Lines)
                {
                    Console.WriteLine(line.Product);
                }
                Console.WriteLine("---------------------");
                Console.WriteLine("Company Details");
                Console.WriteLine(company.Name);
                Console.WriteLine("---------------------");
                Console.WriteLine("Employee Details");
                Console.WriteLine(employee.FirstName + " " + employee.LastName);
                Console.WriteLine("---------------------");
                Console.WriteLine("Order Line Details");
                foreach(var line in order.Lines)
                {
                    Console.WriteLine("Price per unit: " + line.PricePerUnit);
                    Product product = session.Load<Product>(line.Product);
                    Console.WriteLine(product.Name);
                }
                Console.WriteLine("------------------------------------------");
                // --------------------- END Loading Related Documents
            }
            

            // --------------------- Create collection based on Model and retrieve its records --------------------- //
            using (var session = DocumentStoreSingleton.Store.OpenSession())
            {
                Example example = new Example()
                {
                    Desc = "not so random description"
                };

                DerivedExample derivedExample = new DerivedExample()
                {
                    Desc = "This is a derived example. :)",
                    SubDesc = "hmm, work we must."
                };

                session.Store(example);
                Console.WriteLine("stored an Example object");
                session.Store(derivedExample);
                Console.WriteLine("stored a DerivedExample object.");
                session.SaveChanges();


                Console.WriteLine("Retrieving Example objects");

                //var queriedExample = session.Query<Example>()
                //                          .ToList();
                new Examples_ByDesc().Execute(DocumentStoreSingleton.Store);
                var queriedExample = session.Query<Example, Examples_ByDesc>().ToList();
                
                foreach(var exam in queriedExample)
                {
                    Console.WriteLine(exam.Id + " " + exam.Desc);
                }
                Console.WriteLine("------------------------------------------");
            }
            // --------------------- END Create collection based on Model and retrieve its records


            // --------------------- Query Example --------------------- //
            using (var session = DocumentStoreSingleton.Store.OpenSession())
            {
                // Random order id
                Random randomIntGenerator = new Random();
                int companyId = randomIntGenerator.Next(1, 91);

                Console.WriteLine("It's about to get lit fam. We bout to query the orders based on the company id.");
                Console.WriteLine($"By the way, the Company ID is: {companyId}");
                var orders =
                (
                    from order in session.Query<Order>()
                                         .Include(ord => ord.Company)
                    where order.Company == $"companies/{companyId}"
                    select order
                ).ToList();

                foreach(var order in orders)
                {                    
                    Console.WriteLine($"{order.Id} - {order.OrderedAt}");
                }
            }
            // --------------------- END Query Example


#if DEBUG
            Console.WriteLine("press any key to continue...");
            Console.ReadKey();
#endif
        }
    }

    public class Example
    {
        public string Id { get; set; }
        public string Desc { get; set; }
    }

    public class DerivedExample : Example
    {
        public string SubDesc { get; set; }
    }

    public class Examples_ByDesc : AbstractMultiMapIndexCreationTask
    {
        public Examples_ByDesc()
        {
            AddMap<Example>(examples => from e in examples select new { e.Desc });
            AddMap<DerivedExample>(dexams => from de in dexams select new { de.Desc });
        }
    }
}
