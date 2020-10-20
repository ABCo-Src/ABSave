using Microsoft.Diagnostics.Tracing.Parsers.AspNet;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Testing.ConsoleApp
{
    public class Universe
    {
        public static Universe GenerateUniverse()
        {
            return new Universe()
            {
                Planets = new Planet[]
                {
                    new Planet()
                    {
                        PlanetName = "Earth",
                        People = new Person[]
                        {
                            new Person()
                            {
                                Name = "Alex",
                                Age = 15,
                                Jobs = new Job[]
                                {
                                    new Job()
                                    {
                                        Name = "ABSoftware Programmer",
                                        StartTime = new DateTime(2020, 9, 25),
                                        WorkTimeLength = new TimeSpan(6, 0, 0),
                                        Payment = new JobPayment()
                                        {
                                            PaymentFrequency = JobPaymentFrequency.Weekly,
                                            PaymentSize = 50000d
                                        }
                                    },
                                    new Job()
                                    {
                                        Name = "ABSoftware Discord Moderator",
                                        StartTime = new DateTime(2020, 10, 30),
                                        WorkTimeLength = new TimeSpan(24, 0, 0),
                                        Payment = new JobPayment()
                                        {
                                            PaymentFrequency = JobPaymentFrequency.Yearly,
                                            PaymentSize = 5d
                                        }
                                    },
                                },
                                Hobbies = new string[]
                                {
                                    "Programming",
                                    "Video Editing"
                                }
                            },
                            new Person()
                            {
                                Name = "Tom",
                                Age = 48,
                                Jobs = new Job[]
                                {
                                    new Job()
                                    {
                                        Name = "ABSoftware Shop Keeper",
                                        StartTime = new DateTime(2030, 7, 20),
                                        WorkTimeLength = new TimeSpan(8, 0, 0),
                                        Payment = new JobPayment()
                                        {
                                            PaymentFrequency = JobPaymentFrequency.Yearly,
                                            PaymentSize = 500d
                                        }
                                    }
                                }
                            }
                        },
                        Plants = new Plant[]
                        {
                            new Plant()
                            {
                                PlantSize = new ABSize()
                                {
                                    Width = 10,
                                    Height = 300,
                                },
                                LeafCount = 50
                            },
                            new Plant()
                            {
                                PlantSize = new ABSize()
                                {
                                    Width = 100,
                                    Height = 5000,
                                },
                                LeafCount = 50
                            }
                        },
                        Cities = new City[]
                        {
                            new City()
                            {
                                Name = "ABCity",
                                Buildings = new List<Building>()
                                {
                                    new Building()
                                    {
                                        Name = "ABBuilding",
                                        BuildingSize = new ABSize()
                                        {
                                            Width = 1000,
                                            Height = 10000
                                        }
                                    },
                                    new Building()
                                    {
                                        Name = "SomethingElse",
                                        BuildingSize = new ABSize()
                                        {
                                            Width = 200,
                                            Height = 1000
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new Planet()
                    {
                        PlanetName = "Mars",
                        People = null,
                        Plants = new Plant[]
                        {
                            new Plant()
                            {
                                LeafCount = 0,
                                PlantSize = new ABSize()
                                {
                                    Width = 10,
                                    Height = 50
                                }
                            }
                        },
                        Cities = null
                    }
                }
            };
        }

        public Planet[] Planets { get; set; }
    }

    [Serializable]
    public class Planet
    {
        public string PlanetName { get; set; }

        public Person[] People { get; set; }

        public Plant[] Plants { get; set; }

        public City[] Cities { get; set; }
    }

    [Serializable]
    public class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public Job[] Jobs { get; set; }

        public string[] Hobbies { get; set; } = new string[]
        {
            "Programming",
            "SomethingElse"
        };
    }

    [Serializable]
    public class Job
    {
        public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan WorkTimeLength { get; set; }
        public JobPayment Payment { get; set; }
    }

    public struct JobPayment
    {
        public JobPaymentFrequency PaymentFrequency { get; set; }
        public double PaymentSize { get; set; }
    }

    public enum JobPaymentFrequency
    {
        Weekly,
        Monthly,
        Yearly
    }

    public class Plant
    {
        public int LeafCount { get; set; }
        public ABSize PlantSize { get; set; }
    }

    [Serializable]
    public class City
    {
        public string Name { get; set; }

        public List<Building> Buildings { get; set; }
    }

    [Serializable]
    public class Building
    {
        public string Name { get; set; }

        public ABSize BuildingSize { get; set; }
    }

    [Serializable]
    public struct ABSize
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
