import type de from './de'

const en: typeof de = {
  nav: { now: 'Now', songs: 'Songs', queue: 'Queue', players: 'Players', more: 'More' },
  common: { cancel: 'Cancel', save: 'Save', saved: 'Saved', close: 'Close', back: 'Back', retry: 'Try again', loading: 'Loading…' },
  errors: {
    offline: 'No connection to Vocaluxe',
    forbidden: 'You are not allowed to do this',
    busy: 'Vocaluxe is busy, please try again',
    startup: 'Vocaluxe cannot be reached. Is the game running with the server switched on?'
  },
  login: {
    title: 'Who are you?',
    subtitle: 'Pick your profile',
    newProfile: 'New profile',
    name: 'Name',
    pinOptional: 'PIN (optional, at least 4 characters)',
    create: 'Create and start',
    pinTitle: 'PIN for {name}',
    pin: 'PIN',
    enter: 'Log in',
    wrongPin: 'Wrong PIN',
    locked: 'Too many attempts, please wait a moment',
    scanQr: 'Please scan the QR code that Vocaluxe shows under Options → Server.',
    noProfiles: 'There are no profiles yet.'
  },
  now: {
    title: 'Now',
    screen: 'Screen',
    song: 'Song',
    noSong: 'No song loaded',
    upNext: 'Up next',
    remote: 'Remote'
  },
  screens: {
    Main: 'Main menu',
    Song: 'Song selection',
    Sing: 'Singing',
    Score: 'Score',
    Highscore: 'Highscores',
    Names: 'Player selection',
    Profiles: 'Profiles',
    Party: 'Party',
    Jukebox: 'Jukebox',
    Credits: 'Credits',
    Statistics: 'Statistics',
    Load: 'Loading'
  },
  songs: {
    title: 'Songs',
    search: 'Search artist or title',
    allLanguages: 'All languages',
    allDecades: 'All decades',
    decade: '{decade}s',
    onlyDuets: 'Duets only',
    duet: 'Duet',
    sort: 'Sort',
    sortBy: { Artist: 'Artist', Title: 'Title', Year: 'Year', Newest: 'Recently added', Popular: 'Popular' },
    count: 'No songs | 1 song | {n} songs',
    empty: 'No songs found',
    addToQueue: 'Add to queue',
    addedToQueue: '“{title}” is in the queue',
    addToPlaylist: 'Add to playlist',
    addedToPlaylist: 'Added to “{name}”',
    noPlaylists: 'No playlists yet'
  },
  queue: {
    title: 'Queue',
    empty: 'The queue is empty',
    emptyHint: 'Pick something under “Songs”.',
    you: 'You',
    unknown: 'added in the game',
    remove: 'Remove',
    moveUp: 'Move up',
    moveDown: 'Move down',
    removed: 'Removed',
    fairHint: 'Everybody gets one song per round.'
  },
  players: {
    title: 'Players',
    myProfile: 'My profile',
    name: 'Name',
    difficulty: 'Difficulty',
    difficulties: ['Easy', 'Normal', 'Hard'],
    pin: 'PIN',
    newPin: 'New PIN',
    pinSet: 'PIN is set',
    pinNone: 'No PIN',
    removePin: 'Remove PIN',
    pinHint: 'With a PIN nobody else can log in as you.',
    pinTooShort: 'The PIN needs at least 4 characters',
    changeAvatar: 'Change picture',
    allProfiles: 'All profiles',
    role: 'Role',
    roles: { Guest: 'Guest', Player: 'Player', Admin: 'Admin' },
    roleChanged: 'Role changed'
  },
  more: {
    title: 'More',
    playlists: 'Playlists',
    remote: 'Remote',
    admin: 'Settings (admin)',
    language: 'Language',
    logout: 'Log out',
    loggedInAs: 'Logged in as {name}',
    api: 'API documentation'
  },
  playlists: {
    title: 'Playlists',
    new: 'New playlist',
    name: 'Name',
    create: 'Create',
    empty: 'No playlists yet',
    songs: 'No songs | 1 song | {n} songs',
    rename: 'Rename',
    delete: 'Delete',
    deleteConfirm: 'Really delete playlist “{name}”?',
    emptyPlaylist: 'This playlist is empty',
    addHint: 'Add songs via “Songs” → tap a song → “Add to playlist”.'
  },
  remote: {
    title: 'Remote',
    text: 'Type text',
    send: 'Send',
    keys: { Enter: 'OK', Escape: 'Back', Tab: 'Tab', Backspace: 'Delete', Space: 'Space', PageUp: 'Page ↑', PageDown: 'Page ↓' }
  },
  admin: {
    title: 'Settings',
    joining: 'Logging in',
    requireJoinToken: 'QR code only',
    requireJoinTokenHint: 'Only devices that scanned the QR code in Vocaluxe can log in.',
    allowProfileCreation: 'Allow new profiles on the login page',
    queuePlaylistName: 'Name of the queue playlist',
    permissions: 'Permissions',
    permissionsHint: 'Admins may always do everything.',
    perms: {
      AddToQueue: 'Wish songs',
      ManageQueue: 'Manage queue (remove others’ wishes, reorder)',
      UseKeyboard: 'Use the remote',
      EditPlaylists: 'Create and edit playlists',
      DeletePlaylists: 'Delete playlists',
      EditOwnProfile: 'Edit own profile',
      EditAllProfiles: 'Edit all profiles',
      CreateProfiles: 'Create profiles'
    }
  }
}

export default en
