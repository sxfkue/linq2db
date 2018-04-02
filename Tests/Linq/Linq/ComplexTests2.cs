﻿using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;
using Tests.Model;


namespace Tests.ComplexTests2
{
	/// <summary>
	/// Tests:
	/// 
	/// Complex Property Mapping
	/// Inheritance Mapping
	/// LoadWith for Inheritance
	/// LoadWith with Casts
	/// Nested LoadWith
	/// String Enums
	/// Converters for Enums
	/// </summary>
	[TestFixture]
	public class ComplexTests2 : TestBase
	{
		public enum AnimalType
		{
			Small,
			Big
		}

		public enum AnimalType2
		{
			Small,
			Big
		}

		public class Eye
		{
			public int Id { get; set; }
			public string Xy { get; set; }
		}

		public class Name
		{
			public string First { get; set; }
			public string Second { get; set; }
		}

		public class Animal
		{
			public AnimalType AnimalType { get; set; }

			public AnimalType2 AnimalType2 { get; set; }

			public int Id { get; set; }

			public string Name { get; set; }

			public string Discriminator { get; set; }
		}

		public class WildAnimal : Animal
		{
			public WildAnimal()
			{
				Discriminator = "WildAnimal";
			}
		}

		public class SuperWildAnimal : WildAnimal
		{
			public SuperWildAnimal()
			{
				Discriminator = "SuperWildAnimal";
			}
		}

		public class Dog : SuperWildAnimal
		{
			public Dog()
			{
				Discriminator = "Dog";
			}

			public Eye Bla { get; set; }

			public int? EyeId { get; set; }

			public Name DogName { get; set; }
		}

		public class BadDog : Dog
		{
		}

		public class SuperBadDog : BadDog
		{
		}

		public class Test
		{
			public int Id { get; set; }
			public int? TestAnimalId { get; set; }
			public Animal TestAnimal { get; set; }
		}

		private void InsertData()
		{
			var eye = new Eye
			{
				Id = 1,
				Xy = "Hallo"
			};

			var dog = new Dog
			{
				Id = 1,
				Discriminator = "Dog",
				EyeId = 1,
				Name = "FirstDog",
				DogName = new Name { First = "a", Second = "b" },
				AnimalType = AnimalType.Big,
				AnimalType2 = AnimalType2.Big
			};

			var test = new Test
			{
				Id = 1,
				TestAnimalId = null
			};

			var test2 = new Test
			{
				Id = 2,
				TestAnimalId = 1
			};

			using (var db = new TestDataConnection())
			{
				db.DropTable<Animal>();
				db.DropTable<Eye>();
				db.DropTable<Test>();
				db.CreateTable<Animal>();
				db.CreateTable<Eye>();
				db.CreateTable<Test>();
			}

			using (var db = new TestDataConnection())
			{
				db.Insert(eye);
				db.Insert(dog);
				db.Insert(test);
				db.Insert(test2);
			}
		}

		private MappingSchema SetMappings()
		{
			var ms = new MappingSchema();
			ms.SetConverter<AnimalType, string>((obj) =>
			{
				return obj.ToString();
			});
			ms.SetConverter<AnimalType, DataParameter>((obj) =>
			{
				return new DataParameter { Value = obj.ToString() };
			});
			ms.SetConverter<string, AnimalType>((txt) =>
			{
				return (AnimalType)Enum.Parse(typeof(AnimalType), txt, true);
			});
			ms.SetDefaultFromEnumType(typeof(AnimalType2), typeof(string));

			var mappingBuilder = ms.GetFluentMappingBuilder();
			mappingBuilder.Entity<Animal>()
				.HasTableName("Animals")
				.Inheritance(x => x.Discriminator, "Dog", typeof(Dog))
				.Inheritance(x => x.Discriminator, "WildAnimal", typeof(WildAnimal))
				.Inheritance(x => x.Discriminator, "SuperWildAnimal", typeof(SuperWildAnimal))
				.Property(x => x.Name).IsColumn().IsNullable().HasColumnName("Name")
				.Property(x => x.AnimalType).IsColumn().HasColumnName("AnimalType").HasDataType(DataType.NVarChar)
				.Property(x => x.AnimalType2).IsColumn().HasColumnName("AnimalType2").HasDataType(DataType.NVarChar)
				.Property(x => x.Discriminator).IsDiscriminator().IsColumn().IsNullable(false).HasColumnName("Discriminator")
				.Property(x => x.Id).IsColumn().IsNullable(false).HasColumnName("Id").IsPrimaryKey();

			mappingBuilder.Entity<Dog>()
				.HasTableName("Animals")
				.Property(x => x.Bla).IsNotColumn()
				.Property(x => x.EyeId).IsColumn().IsNullable().HasColumnName("EyeId")
				.Property(x => x.DogName.Second).HasColumnName("Second")
				.Property(x => x.DogName.First).HasColumnName("First")
				.Association(x => x.Bla, x => x.EyeId, x => x.Id);

			mappingBuilder.Entity<WildAnimal>()
				.HasTableName("Animals");

			mappingBuilder.Entity<SuperWildAnimal>()
				.HasTableName("Animals");

			mappingBuilder.Entity<Eye>()
				.HasTableName("Eyes")
				.Property(x => x.Id).IsColumn().HasColumnName("Id")
				.Property(x => x.Xy).IsColumn().IsNullable().HasColumnName("Xy");

			mappingBuilder.Entity<Test>()
				.HasTableName("Test")
				.Association(x => x.TestAnimal, x => x.TestAnimalId, x => x.Id)
				.Property(x => x.TestAnimalId).IsColumn().IsNullable().HasColumnName("TestAnimalId")
				.Property(x => x.TestAnimal).IsNotColumn();

			return ms;
		}

		[Test, DataContextSource]
		public void Test1(string context)
		{
			var ms = SetMappings();
			InsertData();

			using (var db = GetDataContext(context, ms))
			{
				var data =  db.GetTable<Animal>().ToList();
				Assert.Null(((Dog)data.First()).Bla);
			}
		}

		[Test, DataContextSource]
		private void Test2(string context)
		{
			var ms = SetMappings();
			InsertData();

			using (var db = GetDataContext(context, ms))
			{
				var data = db.GetTable<Animal>().LoadWith(x => ((Dog)x).Bla).ToList();
				Assert.NotNull(((Dog)data.First()).Bla);
			}
		}

		[Test, DataContextSource]
		public void Test3(string context)
		{
			var ms = SetMappings();
			InsertData();

			using (var db = GetDataContext(context, ms))
			{
				var data = db.GetTable<Test>().LoadWith(x => ((Dog)x.TestAnimal).Bla).ToList();

				Assert.Null(data.First().TestAnimal);
				Assert.NotNull(((Dog)data.Skip(1).First().TestAnimal).Bla);
				Assert.NotNull(((Dog)data.Skip(1).First().TestAnimal).DogName.First);
				Assert.NotNull(((Dog)data.Skip(1).First().TestAnimal).DogName.Second);
			}
		}

		[Test, DataContextSource]
		private void Test4(string context)
		{
			var ms = SetMappings();
			InsertData();

			using (var db = GetDataContext(context, ms))
			{
				var data = db.GetTable<Dog>().ToList();

				Assert.NotNull(data[0].DogName.First);
				Assert.NotNull(data[0].DogName.Second);
			}
		}

		[Test, DataContextSource]
		public void Test5(string context)
		{
			var ms = SetMappings();
			InsertData();

			using (var db = GetDataContext(context, ms))
			{
				var d = new Dog() { AnimalType = AnimalType.Big, AnimalType2 = AnimalType2.Big };

				var test1 = db.GetTable<Dog>().First(x => x.AnimalType == AnimalType.Big);
				var test2 = db.GetTable<Dog>().First(x => x.AnimalType == d.AnimalType);

				var test3 = db.GetTable<Dog>().First(x => x.AnimalType2 == AnimalType2.Big);
				var test4 = db.GetTable<Dog>().First(x => x.AnimalType2 == d.AnimalType2);

				var test6 = db.GetTable<Animal>().First(x => x is SuperWildAnimal);

				var test7 = db.GetTable<Test>().First(x => x.TestAnimal is Dog && ((Dog)x.TestAnimal).EyeId == 1);
			}
		}

		[Test, DataContextSource]
		public void Test6(string context)
		{
			var ms = SetMappings();
			InsertData();

			using (var db = GetDataContext(context, ms))
			{
				var dog = db.GetTable<Dog>().First();
				db.Update(dog);
				db.Update((Animal)dog);

				//var bdog = new SuperBadDog();
				//db.Insert((Dog)bdog);   //this is not possible with my change -> ;-( should it be?
			}
		}
	}
}
