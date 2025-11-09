# Ohjelman käyttötarkoitus
Deadline Tracker -sovellus on suunniteltu seuraamaan projektien ja tehtävien aikatauluja sekä niiden kuvauksia.
Sen avulla käyttäjät voivat hallita projektejaan, tarkastella tulevia määräaikoja ja lisätä uusia tehtäviä helposti.

# Kirjautumisruutu
Kirjautumisruudussa käyttäjä kirjautuu sisään SQL-tietokantaan tallennetulla käyttäjätunnuksella.
Demo-versiossa, jos käyttäjää ei vielä ole, sovellus luo tunnuksen automaattisesti.
Tulevissa versioissa käyttäjät voivat kirjautua omilla, valmiiksi luoduilla tunnuksillaan.

# Yleisnäkymä
Yleisnäkymässä käyttäjä näkee:
- Kaikki omat projektit
- Projektien tehtävät ja aikataulut
- Lisäyspainikkeen (+), jolla voi luoda uusia projekteja

Käyttöliittymä on selkeä ja tarkoitettu nopeaan projektien hallintaan.

# Create Project -ikkuna
Create Project -ikkunassa käyttäjä voi luoda uuden projektin.
- Projektin nimi on pakollinen kenttä.
- Kun painat Tallenna, projekti tallennetaan ja näkyy käyttäjän yleisnäkymässä — edellyttäen, että siihen on lisätty henkilöt.

# Tulevat ominaisuudet
- Käyttäjäkohtaiset asetukset
- Deadline-muistutukset
- Tiimien hallinta ja roolitus
- Visuaalinen aikajana


───────────────────────────────────────────────
# Deadline Tracker (English)

# Purpose
Deadline Tracker is an application designed to track project and task schedules along with their descriptions.
It helps users manage their projects, view upcoming deadlines, and add new tasks efficiently.

# Login Screen
The login screen allows users to sign in with a username stored in an SQL database.
In the demo version, if a user does not exist, the app automatically creates one.
In future versions, users will be able to log in using their pre-registered accounts.

# Main View
The main view displays:
- The user's projects
- Their tasks and schedules
- An add button (+) for creating new projects

The interface is designed for clarity and quick project management.

# Create Project Window
From the Create Project window, you can add new projects.
- The project name field is required.
- When you click Save, the project is added to the user's main view — once members have been added.

# Upcoming Features
- User preferences and customization
- Deadline reminders
- Team management and roles
- Visual timeline and progress tracking
"""