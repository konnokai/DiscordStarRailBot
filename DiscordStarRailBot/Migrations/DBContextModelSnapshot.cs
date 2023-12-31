﻿// <auto-generated />
using System;
using DiscordStarRailBot.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DiscordStarRailBot.Migrations
{
    [DbContext(typeof(DBContext))]
    partial class DBContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.10");

            modelBuilder.Entity("DiscordStarRailBot.DataBase.Table.PlayerIdLink", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("DateAdded")
                        .HasColumnType("TEXT");

                    b.Property<string>("PlayerId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("PlayerIds");
                });

            modelBuilder.Entity("DiscordStarRailBot.DataBase.Table.UserGachaRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("DateAdded")
                        .HasColumnType("TEXT");

                    b.Property<uint>("PickUpCount")
                        .HasColumnType("INTEGER");

                    b.Property<uint>("ThreeStarCount")
                        .HasColumnType("INTEGER");

                    b.Property<uint>("TotalGachaCount")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("UserGachaRecord");
                });

            modelBuilder.Entity("DiscordStarRailBot.DataBase.Table.UserGacheCharacterRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CharacterId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("DateAdded")
                        .HasColumnType("TEXT");

                    b.Property<int>("Num")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("UserGacheCharacterRecord");
                });
#pragma warning restore 612, 618
        }
    }
}
