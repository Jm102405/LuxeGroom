
CREATE TABLE Users (
    UserID       VARCHAR(20) NOT NULL PRIMARY KEY,
    Username     NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Gmail        NVARCHAR(150) NOT NULL UNIQUE,
    Role         NVARCHAR(20) NOT NULL,
    Status       NVARCHAR(20) NOT NULL DEFAULT 'Active',
    DateCreated  DATETIME NOT NULL DEFAULT GETDATE(),
    PhoneNumber NVARCHAR(20) NULL,
    ResetCode NVARCHAR(6) NULL,
    ResetCodeExpiry DATETIME NULL
);

Select*From Users


Select * From Reservations

CREATE TABLE Reservations (
    id               NVARCHAR(20)  PRIMARY KEY,
    owner_name       NVARCHAR(100) NOT NULL,
    pet_name         NVARCHAR(100) NOT NULL,
    grooming_style   NVARCHAR(50)  NOT NULL,
    phone            NVARCHAR(20)  NOT NULL,
    email            NVARCHAR(100) NOT NULL,
    reservation_date DATE          NOT NULL,
    status           NVARCHAR(50)  NOT NULL DEFAULT 'Pending',
    customer_id      NVARCHAR(20)  NULL
);

Drop Table Reservations

