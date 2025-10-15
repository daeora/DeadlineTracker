-- MySQL Workbench Forward Engineering

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';

-- -----------------------------------------------------
-- Schema mydb
-- -----------------------------------------------------

-- -----------------------------------------------------
-- Schema mydb
-- -----------------------------------------------------
CREATE SCHEMA IF NOT EXISTS `mydb` DEFAULT CHARACTER SET utf8 ;
USE `mydb` ;

-- -----------------------------------------------------
-- Table `mydb`.`projekti`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `mydb`.`projekti` (
  `projekti_id` INT NOT NULL AUTO_INCREMENT,
  `projektiNimi` VARCHAR(120) NOT NULL,
  `kuvausTeksti` TEXT NOT NULL,
  `alkupvm` DATE NOT NULL,
  `loppupvm` DATE NOT NULL,
  `luotupvm` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `paivitettypvm` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`projekti_id`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `mydb`.`user`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `mydb`.`user` (
  `user_id` INT NOT NULL AUTO_INCREMENT,
  `kayttajaNimi` VARCHAR(50) NOT NULL,
  `luotupvm` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`user_id`),
  UNIQUE INDEX `user.kayttajaNimi` (`kayttajaNimi` ASC) INVISIBLE)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `mydb`.`projekti_osallistuja`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `mydb`.`projekti_osallistuja` (
  `osallistuja_id` INT NOT NULL AUTO_INCREMENT,
  `projekti_id` INT NOT NULL,
  `user_id` INT NOT NULL,
  `liittynytpvm` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`osallistuja_id`),
  UNIQUE INDEX `ug_project_user` (`projekti_id` ASC, `user_id` ASC) VISIBLE,
  INDEX `fk_pp_user_idx` (`user_id` ASC) VISIBLE,
  CONSTRAINT `fk_pp_project`
    FOREIGN KEY (`projekti_id`)
    REFERENCES `mydb`.`projekti` (`projekti_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `fk_pp_user`
    FOREIGN KEY (`user_id`)
    REFERENCES `mydb`.`user` (`user_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `mydb`.`tehtava`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `mydb`.`tehtava` (
  `tehtava_id` INT NOT NULL AUTO_INCREMENT,
  `projekti_id` INT NOT NULL COMMENT 'FK projekti',
  `tehtavaNimi` VARCHAR(200) NOT NULL,
  `tehtavaKuvaus` TEXT NULL,
  `onValmis` TINYINT(1) NOT NULL DEFAULT 0,
  `luotupvm` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `paivitettypvm` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `erapaiva` DATE NULL,
  `prioriteetti` ENUM('matala', 'keskitaso', 'tärkeä') NOT NULL,
  `maarattyHenkilolle` INT NULL,
  PRIMARY KEY (`tehtava_id`),
  UNIQUE INDEX `unique_project_task` (`projekti_id` ASC, `tehtavaNimi` ASC) INVISIBLE,
  INDEX `fk_tehtava_kayttaja_idx` (`maarattyHenkilolle` ASC) VISIBLE,
  CONSTRAINT `fk_tehtava_kayttaja`
    FOREIGN KEY (`maarattyHenkilolle`)
    REFERENCES `mydb`.`user` (`user_id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
  CONSTRAINT `fk_tehtava_projekti`
    FOREIGN KEY (`projekti_id`)
    REFERENCES `mydb`.`projekti` (`projekti_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB;


SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
