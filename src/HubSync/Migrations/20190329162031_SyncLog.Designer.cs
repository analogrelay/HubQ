﻿// <auto-generated />
using System;
using HubSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HubSync.Migrations
{
    [DbContext(typeof(HubSyncContext))]
    [Migration("20190329162031_SyncLog")]
    partial class SyncLog
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.0.0-preview3.19153.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("HubSync.Models.SyncLogEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTimeOffset?>("Completed");

                    b.Property<string>("Owner")
                        .IsRequired();

                    b.Property<string>("Repository")
                        .IsRequired();

                    b.Property<DateTimeOffset>("Started");

                    b.Property<string>("User")
                        .IsRequired();

                    b.Property<DateTimeOffset>("WaterMark");

                    b.HasKey("Id");

                    b.ToTable("SyncLog");
                });
#pragma warning restore 612, 618
        }
    }
}