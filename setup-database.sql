-- Script tạo database cho EV Charging Station System
-- Chạy script này trong SQL Server Management Studio hoặc Azure Data Studio

-- Tạo database cho User Service
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EVChargingStation_User')
BEGIN
    CREATE DATABASE [EVChargingStation_User]
END
GO

-- Tạo database cho Station Service
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EVChargingStation_Station')
BEGIN
    CREATE DATABASE [EVChargingStation_Station]
END
GO

-- Tạo database cho Booking Service
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EVChargingStation_Booking')
BEGIN
    CREATE DATABASE [EVChargingStation_Booking]
END
GO

-- Tạo database cho Payment Service
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EVChargingStation_Payment')
BEGIN
    CREATE DATABASE [EVChargingStation_Payment]
END
GO

-- Tạo database cho Notification Service
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EVChargingStation_Notification')
BEGIN
    CREATE DATABASE [EVChargingStation_Notification]
END
GO

-- Tạo database cho Report Service
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EVChargingStation_Report')
BEGIN
    CREATE DATABASE [EVChargingStation_Report]
END
GO

PRINT 'Tất cả database đã được tạo thành công!'





