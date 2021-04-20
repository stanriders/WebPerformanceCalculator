﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WebPerformanceCalculator.DB;

namespace WebPerformanceCalculator.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.4");

            modelBuilder.Entity("WebPerformanceCalculator.DB.Types.Map", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("MaxCombo")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Maps");
                });

            modelBuilder.Entity("WebPerformanceCalculator.DB.Types.Player", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Country")
                        .HasColumnType("TEXT");

                    b.Property<double>("LivePp")
                        .HasColumnType("REAL");

                    b.Property<double>("LocalPp")
                        .HasColumnType("REAL");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<double>("PlaycountPp")
                        .HasColumnType("REAL");

                    b.Property<DateTime>("UpdateTime")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Players");
                });

            modelBuilder.Entity("WebPerformanceCalculator.DB.Types.Score", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<double>("Accuracy")
                        .HasColumnType("REAL");

                    b.Property<string>("AdditionalPpData")
                        .HasColumnType("TEXT");

                    b.Property<int>("Combo")
                        .HasColumnType("INTEGER");

                    b.Property<double?>("LivePp")
                        .HasColumnType("REAL");

                    b.Property<double>("LocalPp")
                        .HasColumnType("REAL");

                    b.Property<int>("MapId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Misses")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Mods")
                        .HasColumnType("TEXT");

                    b.Property<int>("PlayerId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PositionChange")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("UpdateTime")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("MapId");

                    b.HasIndex("PlayerId");

                    b.ToTable("Scores");
                });

            modelBuilder.Entity("WebPerformanceCalculator.DB.Types.Score", b =>
                {
                    b.HasOne("WebPerformanceCalculator.DB.Types.Map", "Map")
                        .WithMany()
                        .HasForeignKey("MapId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("WebPerformanceCalculator.DB.Types.Player", "Player")
                        .WithMany("Scores")
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Map");

                    b.Navigation("Player");
                });

            modelBuilder.Entity("WebPerformanceCalculator.DB.Types.Player", b =>
                {
                    b.Navigation("Scores");
                });
#pragma warning restore 612, 618
        }
    }
}
