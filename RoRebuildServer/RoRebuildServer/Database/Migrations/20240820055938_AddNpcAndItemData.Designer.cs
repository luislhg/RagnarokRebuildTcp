﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RoRebuildServer.Database;

#nullable disable

namespace RoRebuildServer.Migrations
{
    [DbContext(typeof(RoContext))]
    [Migration("20240820055938_AddNpcAndItemData")]
    partial class AddNpcAndItemData
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

            modelBuilder.Entity("RoRebuildServer.Database.Domain.DbCharacter", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Data")
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("ItemData")
                        .HasColumnType("BLOB");

                    b.Property<string>("Map")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("NpcFlags")
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("SkillData")
                        .HasColumnType("BLOB");

                    b.Property<int>("X")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Y")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Character");
                });

            modelBuilder.Entity("RoRebuildServer.Database.Domain.DbCharacter", b =>
                {
                    b.OwnsOne("RoRebuildServer.Database.Domain.DbSavePoint", "SavePoint", b1 =>
                        {
                            b1.Property<Guid>("DbCharacterId")
                                .HasColumnType("TEXT");

                            b1.Property<int>("Area")
                                .HasColumnType("INTEGER");

                            b1.Property<string>("MapName")
                                .HasColumnType("TEXT");

                            b1.Property<int>("X")
                                .HasColumnType("INTEGER");

                            b1.Property<int>("Y")
                                .HasColumnType("INTEGER");

                            b1.HasKey("DbCharacterId");

                            b1.ToTable("Character");

                            b1.WithOwner()
                                .HasForeignKey("DbCharacterId");
                        });

                    b.Navigation("SavePoint");
                });
#pragma warning restore 612, 618
        }
    }
}
