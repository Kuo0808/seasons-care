# Project Domain

## Project Name

- seasons-care

## Project Type

- 協作式長照平台
- Collaborative Care Platform

## Core Concept

系統以 Care Group 為核心。

定義：

- 1 Care Group = 1 Care Recipient

## Multi-Tenancy Concept

所有與照護相關的資料，必須依據 `care_group_id` 進行資料隔離。

## Main Domain Objects

- Users
- Care Groups
- Care Group Members
- Care Logs
- Health Records
- Medications
- Tasks
- Notifications

## Domain Principle

本系統的設計目標是讓多位照護參與者可以共同協作、共享紀錄、追蹤健康資訊，並降低照護溝通成本。