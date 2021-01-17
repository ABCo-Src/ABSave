using MessagePack;
using Microsoft.Diagnostics.Tracing.Parsers.AspNet;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Testing.ConsoleApp
{
    [MessagePackObject]
    [Serializable]
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

        [Key(0)]
        public virtual Planet[] Planets { get; set; }
    }

    [MessagePackObject]
    [Serializable]
    public class Planet
    {
        [Key(0)]
        public virtual string PlanetName { get; set; }

        [Key(1)]
        public virtual Person[] People { get; set; }

        [Key(2)]
        public virtual Plant[] Plants { get; set; }

        [Key(3)]
        public virtual City[] Cities { get; set; }
    }

    [MessagePackObject]
    [Serializable]
    public class Person
    {
        [Key(0)]
        public virtual string Name { get; set; }

        [Key(1)]
        public virtual int Age { get; set; }

        [Key(2)]
        public virtual Job[] Jobs { get; set; }

        [Key(3)]
        public virtual string[] Hobbies { get; set; } = new string[]
        {
            "Programming",
            "SomethingElse"
        };
    }

    [MessagePackObject]
    [Serializable]
    public class Job
    {
        [Key(0)]
        public virtual string Name { get; set; }

        [Key(1)]
        public virtual DateTime StartTime { get; set; }

        [Key(2)]
        public virtual TimeSpan WorkTimeLength { get; set; }

        [Key(3)]
        public virtual JobPayment Payment { get; set; }
    }

    [MessagePackObject]
    [Serializable]
    public struct JobPayment
    {
        [Key(0)]
        public JobPaymentFrequency PaymentFrequency { get; set; }

        [Key(1)]
        public double PaymentSize { get; set; }
    }

    public enum JobPaymentFrequency
    {
        Weekly,
        Monthly,
        Yearly
    }

    [MessagePackObject]
    [Serializable]
    public class Plant
    {
        [Key(0)]
        public virtual int LeafCount { get; set; }

        [Key(1)]
        public virtual ABSize PlantSize { get; set; }
    }

    [MessagePackObject]
    [Serializable]
    public class City
    {
        [Key(0)]
        public virtual string Name { get; set; }

        [Key(1)]
        public virtual List<Building> Buildings { get; set; }
    }

    [MessagePackObject]
    [Serializable]
    public class Building
    {
        [Key(0)]
        public virtual string Name { get; set; }

        [Key(1)]
        public virtual ABSize BuildingSize { get; set; }
    }

    [MessagePackObject]
    [Serializable]
    public struct ABSize
    {
        [Key(0)]
        public double Width { get; set; }

        [Key(1)]
        public double Height { get; set; }
    }
}
