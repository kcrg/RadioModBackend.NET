# .NET RadioModBackend API

An alternative API backend for [RadioMod for Mordhau](https://github.com/TheSaltySeaCow/Radio-Mod-Backend), built with the latest **.NET 9** using **Native AOT**. This allows the application to run as a standalone executable without requiring any runtime software installed. The backend supports output caching for improved performance and integrates with YouTube using the [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) NuGet package.

## Features

- **Standalone Executable**: Built with Native AOT, optimized for size or speed - no runtime installation required.
- **YouTube Integration**: Utilizes `YoutubeExplode` for seamless YouTube API interactions.
- **Output Caching**: Implements `OutputCache` for efficient response caching.
- **Configurable**: Easy setup using a straightforward `appsettings.json` file.
- **Discord Webhook Support**: Optionally send notifications via Discord when a queue action occurs.
- **Ban System**: Ability to ban specific PlayFab IDs, video IDs, or search terms.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
  - [Download Pre-built Executable](#download-pre-built-executable)
  - [Build from Source](#build-from-source)
- [Configuration](#configuration)
  - [Configuration Options](#configuration-options)
- [Usage](#usage)
  - [Running the Application](#running-the-application)
  - [Endpoints](#endpoints)
    - [POST `/Queue`](#post-queue)
    - [POST `/Search`](#post-search)
    - [POST `/playlist`](#post-playlist)
- [Contributing](#contributing)
- [License](#license)

## Prerequisites

- **Operating System**: Windows, Linux, or macOS.
- **.NET 9 SDK** (required only if building from source).

## Installation

### Download Pre-built Executable

Download the latest release optimized for [Size](#) or [Speed](#) from the [Releases](https://github.com/kcrg/RadioModBackend.NET/releases) page.

- **Size Optimization**: Smaller executable size.
- **Speed Optimization**: Faster execution performance.

### Build from Source

1. **Clone the repository**:

   ```bash
   git clone https://github.com/kcrg/RadioModBackend.NET.git
   ```

2. **Navigate to the project directory**:

   ```bash
   cd RadioModBackend.NET/src
   ```

3. **Publish the application**:

   - **For Size Optimization**:

     ```bash
     dotnet publish -c Release -r <RID> /p:PublishAot=true /p:OptimizationPreference=Size
     ```

   - **For Speed Optimization**:

     ```bash
     dotnet publish -c Release -r <RID> /p:PublishAot=true /p:OptimizationPreference=Speed
     ```

   Replace `<RID>` with your Runtime Identifier (e.g., `win-x64`, `linux-x64`).

## Configuration

Create an `appsettings.json` file in the root directory with the following content:

```json
{
  "RadioMod": {
    "Endpoint": "http://yourdomain.com",
    "Port": "3045",
    "EnableWebhookOnQueue": true,
    "MaxSearchCount": 15,
    "BannedVideoIDs": ["videoId01", "videoId02"],
    "BannedTerms": ["fart", "idiot"],
    "BannedPlayfabIDs": ["dfg", "playfabId02"]
  }
}
```

### Configuration Options

- **Endpoint**: The base URL where your API will be accessible.
- **Port**: The port number on which the API will run.
- **EnableWebhookOnQueue**: Enables Discord webhook notifications when a video is queued.
- **MaxSearchCount**: Maximum number of search results to return. Smaller value = faster response.
- **BannedVideoIDs**: List of YouTube video IDs to ban.
- **BannedTerms**: List of terms to ban in video titles.
- **BannedPlayfabIDs**: List of PlayFab IDs to ban from using the API.

## Usage

### Running the Application

Execute the built application:

```bash
./RadioModBackend.NET
```

The application will start and listen on the port specified in the configuration.

### Endpoints

#### POST `/Queue`

Queues a YouTube video for playback.

**Request Body**:

```json
{
  "videoId": "youtubeVideoId",
  "videoTitle": "Video Title",
  "server": "ServerName",
  "playfabId": "player123",
  "playerName": "PlayerName",
  "serverWebhook": "https://discordapp.com/api/webhooks/..."
}
```

- **Fields**:
  - `videoId` (string, required): The YouTube video ID.
  - `videoTitle` (string, required): The title of the video.
  - `server` (string, required): The name of the server.
  - `playfabId` (string, required): The PlayFab ID of the player.
  - `playerName` (string, required): The name of the player.
  - `serverWebhook` (string, required): The Discord webhook URL for notifications.

**Response**:

```json
{
  "valid": true,
  "videoId": "youtubeVideoId",
  "uuid": "http://yourdomain.com:3045/unique-id",
  "maxRes": true,
  "videoTitle": "Video Title",
  "error": null
}
```

- **Fields**:
  - `valid` (bool): Indicates if the request was successful.
  - `videoId` (string): The YouTube video ID.
  - `uuid` (string): The URL to access the cached video.
  - `maxRes` (bool): Indicates if the maximum resolution is available.
  - `videoTitle` (string): The title of the video.
  - `error` (string): Error message if `valid` is `false`.

**Notes**:

- Validates against banned PlayFab IDs, video IDs, and terms.
- Sends a Discord webhook if `serverWebhook` is provided and `EnableWebhookOnQueue` is `true`.

#### POST `/Search`

Searches YouTube for videos matching the search string.

**Request Body**:

```json
{
  "searchString": "search terms"
}
```

- **Fields**:
  - `searchString` (string, required): The terms to search for.

**Response**:

```json
{
  "results": [
    {
      "id": "vcaPiiFZu2o",
      "title": "Ocean Man",
      "timestamp": "2:07",
      "author": "Ween - Topic",
      "ago": "No upload date",
      "views": "23.5M",
      "seconds": 127
    },
    {
      "id": "tkzY_VwNIek",
      "title": "Ween - Ocean Man [Music Video]",
      "timestamp": "2:09",
      "author": "ElectromaMV",
      "ago": "7 years ago",
      "views": "24.2M",
      "seconds": 129
    },
    {
      "id": "6E5m_XtCX3c",
      "title": "Ocean Man Lyrics",
      "timestamp": "2:08",
      "author": "Hedley48",
      "ago": "15 years ago",
      "views": "36M",
      "seconds": 128
    }
    // More search items...
  ]
}
```

- **Fields**:
  - `results` (array): List of search results.
    - Each item contains:
      - `id` (string): The YouTube video ID.
      - `title` (string): The title of the video.
      - `timestamp` (string): Duration of the video in `mm:ss` format.
      - `author` (string): The channel or uploader.
      - `ago` (string): How long ago the video was uploaded.
      - `views` (string): Number of views.
      - `seconds` (int): Duration of the video in seconds.

**Notes**:

- Returns up to `MaxSearchCount` results as specified in the configuration.

#### POST `/playlist`

Retrieves information about a YouTube playlist.

**Headers**:

- `playlistId`: The ID of the YouTube playlist.

**Response**:

```json
{
  "playlistId": "PLi0mNC3SNQe129kBUp3Y6V50W52XBS97j",
  "title": "One True God",
  "results": [
    {
      "id": "SQK7vgFMNFU",
      "title": "One True God & EDDIE - Demons",
      "timestamp": "2:50",
      "author": "One True God",
      "ago": null,
      "views": null,
      "seconds": 170
    },
    {
      "id": "0dVaZ-2Sj1k",
      "title": "REZZ x Virtual Riot x One True God - Give In To You",
      "timestamp": "3:21",
      "author": "One True God",
      "ago": null,
      "views": null,
      "seconds": 201
    }
    // More playlist items...
  ]
}
```

- **Fields**:
  - `playlistId` (string): The YouTube playlist ID.
  - `title` (string): The title of the playlist.
  - `results` (array): List of videos in the playlist.
    - Each item contains:
      - `id` (string): The YouTube video ID.
      - `title` (string): The title of the video.
      - `timestamp` (string): Duration of the video in `mm:ss` format.
      - `author` (string): The channel or uploader.
      - `ago` (string): How long ago the video was uploaded (may be `null`).
      - `views` (string): Number of views (may be `null`).
      - `seconds` (int): Duration of the video in seconds.

**Notes**:

- Requires the `playlistId` header to be set.
- Returns detailed information about each video in the playlist.

## Contributing

Contributions are welcome! Please feel free to open an issue or submit a pull request.

## License

This project is licensed under the [MIT License](LICENSE).

---

**Disclaimer**: This project is a work in progress and is not affiliated with the original RadioMod or Mordhau developers.
