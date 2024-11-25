
# e-Claim POC Setup Guide

This document provides detailed steps for setting up, testing, and troubleshooting the e-Claim Proof of Concept (POC) application.

## Table of Contents
- [e-Claim POC Setup Guide](#e-claim-poc-setup-guide)
  - [Table of Contents](#table-of-contents)
  - [1. Clone or Unzip the Project](#1-clone-or-unzip-the-project)
    - [Step: Obtain the project from the repository or as a compressed file.](#step-obtain-the-project-from-the-repository-or-as-a-compressed-file)
  - [2. Navigate to the Project Directory](#2-navigate-to-the-project-directory)
  - [3. Install Prerequisites](#3-install-prerequisites)
  - [4. Run Docker-Compose](#4-run-docker-compose)
  - [5. Keycloak Setup](#5-keycloak-setup)
    - [Access Keycloak:](#access-keycloak)
    - [Import the Realm:](#import-the-realm)
    - [Add Admin User:](#add-admin-user)
  - [6. Swagger API Documentation](#6-swagger-api-documentation)
    - [Authenticate:](#authenticate)
  - [7. Token Acquisition](#7-token-acquisition)
    - [Method: POST](#method-post)
    - [Parameters:](#parameters)
  - [8. Database Verification](#8-database-verification)
    - [PostgreSQL:](#postgresql)
    - [MongoDB:](#mongodb)
  - [9. Elasticsearch Setup](#9-elasticsearch-setup)
    - [Password Setup:](#password-setup)
    - [Verify Elasticsearch:](#verify-elasticsearch)
    - [Create and Assign Roles:](#create-and-assign-roles)
  - [10. RabbitMQ Event Processing](#10-rabbitmq-event-processing)
    - [Access RabbitMQ:](#access-rabbitmq)
    - [Publish Test Event:](#publish-test-event)
  - [11. Troubleshooting](#11-troubleshooting)
    - [Common Issues:](#common-issues)
  - [12. Final Checklist](#12-final-checklist)

---

## 1. Clone or Unzip the Project

### Step: Obtain the project from the repository or as a compressed file.

Command:  
```bash
git clone https://github.com/nitinrahane/e-claim-poc.git
```

Alternatively, unzip the provided archive of `e-claim-poc`.

---

## 2. Navigate to the Project Directory

Command:  
```bash
cd e-claim-poc
```

---

## 3. Install Prerequisites

Ensure the following are installed:
- **.NET 8 Runtime**: Verify with `dotnet --version`.
- **Docker and Docker-Compose**: Verify with `docker --version` and `docker-compose --version`.

---

## 4. Run Docker-Compose

Command:  
```bash
docker-compose up --build
```

Wait until images are downloaded and containers are up and running. It may take some time to pull the images from the internet.

---

## 5. Keycloak Setup

### Access Keycloak:
- URL: [http://localhost:8080](http://localhost:8080)
- Credentials: `admin / admin`

### Import the Realm:
1. Navigate to **Administration Console** → **Create Realm**.
2. Upload the provided `realm-export.json` file located in the `e-claim-poc/keycloak` folder.
3. Click **Create**.

### Add Admin User:
1. Create an admin user with an admin password.
2. Assign the `Admin` role to the user in the **Role Mapping** tab.
3. Regenerate the client secret for `eclaim-api-client` and update the `kong.yml` file with:
   - **Keycloak public key**: Obtain from **Realm Settings** → Copy the `RS256` public key.
   - Paste it in the `kong/kong.yml` file under `jwt_secrets`.

Restart containers for changes to take effect:
```bash
docker-compose down
docker-compose up --build
```

---

## 6. Swagger API Documentation

Access Swagger:
- [http://localhost:5016/swagger](http://localhost:5016/swagger)
- [http://localhost:5020/swagger](http://localhost:5020/swagger)

### Authenticate:
1. Obtain a token from Keycloak (see [Token Acquisition](#7-token-acquisition)).
2. Use the **Authorize** button in Swagger with `Bearer <token>`.

---

## 7. Token Acquisition

### Method: POST  
URL:  
```bash
http://localhost:8080/realms/eclaim-realm/protocol/openid-connect/token
```

### Parameters:
- `grant_type=password`
- `client_id=eclaim-api-client`
- `username=admin`
- `password=admin`
- `client_secret=<your-client-secret>`

Example command for testing:
```bash
curl -X POST -d "grant_type=password&client_id=eclaim-api-client&username=admin&password=admin&client_secret=<your-client-secret>" http://localhost:8080/realms/eclaim-realm/protocol/openid-connect/token
```

---

## 8. Database Verification

### PostgreSQL:
1. Connect:
   ```bash
   docker-compose exec postgres psql -U claim_user -d claim_db
   ```
2. Verify data:
   ```sql
   SELECT * FROM "Claims";
   ```

### MongoDB:
1. Check if the `mongo` CLI tool is installed:
   ```bash
   docker exec -it e-claim-poc-mongodb-1 ls /usr/bin/mongo
   ```
2. If not, install it:
   ```bash
   docker exec -it e-claim-poc-mongodb-1 bash
   apt-get update
   ```
3. Query data using MongoDB CLI or VS Code extension.

---

## 9. Elasticsearch Setup

### Password Setup:
```bash
docker compose exec elasticsearch bin/elasticsearch-setup-passwords interactive
```

### Verify Elasticsearch:
```bash
curl -u elastic:admin@123 http://localhost:9200
```

### Create and Assign Roles:
1. Define `logstash_writer` role:
   ```bash
   curl -X POST -u elastic:your_elastic_password http://localhost:9200/_security/role/logstash_writer    -H "Content-Type: application/json"    -d '{
       "cluster": ["monitor", "manage_index_templates"],
       "indices": [
           {
               "names": ["logstash-*", "claims-api-logs-*"],
               "privileges": ["create", "write", "create_index", "manage", "auto_configure"]
           }
       ]
   }'
   ```
2. Create `logstash_internal` user:
   ```bash
   curl -X POST -u elastic:your_elastic_password http://localhost:9200/_security/user/logstash_internal    -H "Content-Type: application/json"    -d '{
       "password": "admin@123",
       "roles": ["logstash_writer"]
   }'
   ```

Restart containers for changes to take effect:
```bash
docker-compose down
docker-compose up --build
```

---

## 10. RabbitMQ Event Processing

### Access RabbitMQ:
- URL: [http://localhost:15672](http://localhost:15672)
- Credentials: `guest / guest`

### Publish Test Event:
```bash
curl -u guest:guest -X POST http://localhost:15672/api/exchanges/%2F/claims_exchange/publish -H "Content-Type: application/json" -d '{
  "routing_key": "document.processed",
  "payload": "{"ClaimId":"12345","DocumentId":"67890","CorrelationId":"abcd-1234"}",
  "payload_encoding": "string"
}'
```

---

## 11. Troubleshooting

### Common Issues:
1. **Keycloak Login Failure**: Verify service is running and credentials are correct.
2. **Elasticsearch Errors**: Ensure correct passwords and roles.
3. **RabbitMQ Event Failure**: Check routing keys and logs.

---

## 12. Final Checklist

1. Ensure all services are running (`docker ps`).
2. Test APIs using Swagger or Postman.
3. Verify logs in Elasticsearch and RabbitMQ.
