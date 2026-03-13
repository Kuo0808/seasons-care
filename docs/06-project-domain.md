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
包含但不限於：

- Care Logs
- Health Records
- Medications
- Tasks
- Notifications
- Expense Records

所有查詢、寫入、更新與權限檢查，都必須考慮 `care_group_id` 範圍。

## Main Domain Objects

- Users
- Care Groups
- Care Group Members
- Care Logs
- Health Records
- Medications
- Tasks
- Notifications
- Expense Records

## Main User Roles

### Family Caregiver
家庭共同照護者，可查看與管理同一 Care Group 內的照護資訊。

### Care Recipient
被照護者。本系統資料主要圍繞被照護者所屬的 Care Group 建立。

## MVP Core Features

### 1. Auth
- User Register
- User Login
- JWT Authentication
- Care Group initial binding or creation flow

### 2. Shared Care Logs
- 建立、查看、編輯照護日誌
- 群組成員可共享照護紀錄

### 3. Shared Expense Tracking
- 記錄照護支出
- 群組成員共同查看與分帳

### 4. AI Voice Input
- 將語音轉文字
- 未來可延伸為協助建立、修改、查詢平台資料

## Domain Principle

本系統的設計目標是讓多位照護參與者可以共同協作、共享紀錄、追蹤健康資訊，並降低照護溝通成本。

所有功能設計必須優先考慮：

1. 家庭共同照護協作
2. Care Group 資料隔離
3. 資訊同步與追蹤
4. MVP 可落地實作