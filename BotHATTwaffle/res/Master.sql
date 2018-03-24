begin transaction;

create table if not exists Shitposts (
    seq_id      integer primary key autoincrement,
    snowflake   integer,
    username    text,
    shitpost    text,
    fullmessage text,
    date        integer
);

create table if not exists Servers (
    name          text,
    description   text,
    address       text,
    rcon_password text,
    ftp_path      text,
    ftp_username  text,
    ftp_password  text,
    ftp_type      text,
    primary key (name)
);

create table if not exists SearchDataTags (
    name   text,
    tag    text,
    series text,
    primary key (name, tag, series)
);

create table if not exists Mutes (
    seq_id        integer primary key autoincrement,
    snowflake     integer,
    username      text,
    mute_reason   text,
    mute_duration integer,
    muted_by      text,
    date          integer
);

create table if not exists KeyVaules (
    key   text,
    value text,
    primary key (key)
);

create table if not exists CommandUsage (
    seq_id      integer primary key autoincrement,
    snowflake   integer,
    username    text,
    command     text,
    fullmessage text,
    date        integer
);

create table if not exists ActiveMutes (
    snowflake     integer,
    username      text,
    mute_reason   text,
    mute_duration integer,
    muted_by      text,
    muted_time    integer,
    primary key (snowflake)
);

create table if not exists SearchDataResults (
    name text,
    url  text,
    primary key (name)
);

insert into SearchDataTags values ('v2 1', '1', 'v2');
insert into SearchDataTags values ('v2 1', 'installing', 'v2');
insert into SearchDataTags values ('v2 1', 'configuration', 'v2');
insert into SearchDataTags values ('v2 2', '2', 'v2');
insert into SearchDataTags values ('v2 2', 'sky', 'v2');
insert into SearchDataTags values ('v2 2', 'skybox', 'v2');
insert into SearchDataTags values ('v2 3', '3', 'v2');
insert into SearchDataTags values ('v2 3', 'clipping', 'v2');
insert into SearchDataTags values ('v2 3', 'brush', 'v2');
insert into SearchDataTags values ('v2 4', '4', 'v2');
insert into SearchDataTags values ('v2 4', 'leak', 'v2');
insert into SearchDataTags values ('v2 4', 'leaks', 'v2');
insert into SearchDataTags values ('v2 4', 'lin', 'v2');
insert into SearchDataTags values ('v2 5', '5', 'v2');
insert into SearchDataTags values ('v2 5', 'prop', 'v2');
insert into SearchDataTags values ('v2 5', 'static', 'v2');
insert into SearchDataTags values ('v2 5', 'prop_static', 'v2');
insert into SearchDataTags values ('v2 5', 'prop_dynamic', 'v2');
insert into SearchDataTags values ('v2 6', '6', 'v2');
insert into SearchDataTags values ('v2 6', 'optimization', 'v2');
insert into SearchDataTags values ('v2 6', 'pvs', 'v2');
insert into SearchDataTags values ('v2 6', 'prt', 'v2');
insert into SearchDataTags values ('v2 6', 'func_detail', 'v2');
insert into SearchDataTags values ('v2 7', '7', 'v2');
insert into SearchDataTags values ('v2 7', 'vertex', 'v2');
insert into SearchDataTags values ('v2 7', 'primitive', 'v2');
insert into SearchDataTags values ('v2 8', '8', 'v2');
insert into SearchDataTags values ('v2 8', 'light', 'v2');
insert into SearchDataTags values ('v2 8', 'lighting', 'v2');
insert into SearchDataTags values ('v2 8', 'point_spotlight', 'v2');
insert into SearchDataTags values ('v2 8', 'light_spot', 'v2');
insert into SearchDataTags values ('v2 8', 'env_sprite', 'v2');
insert into SearchDataTags values ('v2 8', 'hdr', 'v2');
insert into SearchDataTags values ('v2 8', 'rad', 'v2');
insert into SearchDataTags values ('v2 8', 'smoothing', 'v2');
insert into SearchDataTags values ('v2 8', 'projectedtexture', 'v2');
insert into SearchDataTags values ('v2 9', '9', 'v2');
insert into SearchDataTags values ('v2 9', 'displacement', 'v2');
insert into SearchDataTags values ('v2 9', 'sew', 'v2');
insert into SearchDataTags values ('v2 10', '10', 'v2');
insert into SearchDataTags values ('v2 10', 'water', 'v2');
insert into SearchDataTags values ('v2 11', '11', 'v2');
insert into SearchDataTags values ('v2 11', 'door', 'v2');
insert into SearchDataTags values ('v2 11', 'prop_door', 'v2');
insert into SearchDataTags values ('v2 11', 'func_door', 'v2');
insert into SearchDataTags values ('v2 12', '12', 'v2');
insert into SearchDataTags values ('v2 12', 'intput', 'v2');
insert into SearchDataTags values ('v2 12', 'output', 'v2');
insert into SearchDataTags values ('v2 12', 'io', 'v2');
insert into SearchDataTags values ('v2 13', '13', 'v2');
insert into SearchDataTags values ('v2 13', 'vmt', 'v2');
insert into SearchDataTags values ('v2 13', 'normal', 'v2');
insert into SearchDataTags values ('v2 13', 'vtf', 'v2');
insert into SearchDataTags values ('v2 13', 'packing', 'v2');
insert into SearchDataTags values ('v2 13', 'pack', 'v2');
insert into SearchDataTags values ('v2 13', 'vide', 'v2');
insert into SearchDataTags values ('v2 13', 'texture', 'v2');
insert into SearchDataTags values ('v2 14', '14', 'v2');
insert into SearchDataTags values ('v2 14', 'cubemap', 'v2');
insert into SearchDataTags values ('v2 14', 'envmap', 'v2');
insert into SearchDataTags values ('v2 14', 'specmask', 'v2');
insert into SearchDataTags values ('v2 15', '15', 'v2');
insert into SearchDataTags values ('v2 15', 'skybox', 'v2');
insert into SearchDataTags values ('v2 15', 'sky_camera', 'v2');
insert into SearchDataTags values ('v2 16', '16', 'v2');
insert into SearchDataTags values ('v2 16', 'fog', 'v2');
insert into SearchDataTags values ('v2 16', 'env_fog_controller', 'v2');
insert into SearchDataTags values ('v2 17', '17', 'v2');
insert into SearchDataTags values ('v2 17', 'color', 'v2');
insert into SearchDataTags values ('v2 17', 'correction', 'v2');
insert into SearchDataTags values ('v2 17', 'raw', 'v2');
insert into SearchDataTags values ('v2 18', '18', 'v2');
insert into SearchDataTags values ('v2 18', 'movelinear', 'v2');
insert into SearchDataTags values ('v2 19', '19', 'v2');
insert into SearchDataTags values ('v2 19', 'parent', 'v2');
insert into SearchDataTags values ('v2 19', 'parenting', 'v2');
insert into SearchDataTags values ('v2 19', 'attachment', 'v2');
insert into SearchDataTags values ('v2 20', '20', 'v2');
insert into SearchDataTags values ('v2 20', 'rotating', 'v2');
insert into SearchDataTags values ('v2 20', 'func_rotating', 'v2');
insert into SearchDataTags values ('v2 20', 'fan', 'v2');
insert into SearchDataTags values ('v2 21', '21', 'v2');
insert into SearchDataTags values ('v2 21', 'optimization', 'v2');
insert into SearchDataTags values ('v2 21', 'func_detail', 'v2');
insert into SearchDataTags values ('v2 21', 'hint', 'v2');
insert into SearchDataTags values ('v2 21', 'skip', 'v2');
insert into SearchDataTags values ('v2 21', 'areaportal', 'v2');
insert into SearchDataTags values ('v2 22', '22', 'v2');
insert into SearchDataTags values ('v2 22', 'sound', 'v2');
insert into SearchDataTags values ('v2 22', 'soundscape', 'v2');
insert into SearchDataTags values ('v2 22', 'ambient_generic', 'v2');
insert into SearchDataTags values ('v2 22', 'script', 'v2');
insert into SearchDataTags values ('v2 23', '23', 'v2');
insert into SearchDataTags values ('v2 23', 'texture', 'v2');
insert into SearchDataTags values ('v2 23', 'alignement', 'v2');
insert into SearchDataTags values ('v2 23', 'justify', 'v2');
insert into SearchDataTags values ('v2 23', 'wrap', 'v2');
insert into SearchDataTags values ('v2 24', '24', 'v2');
insert into SearchDataTags values ('v2 24', 'decal', 'v2');
insert into SearchDataTags values ('v2 24', 'overlay', 'v2');
insert into SearchDataTags values ('v2 24', 'dekale', 'v2');
insert into SearchDataTags values ('v2 25', '25', 'v2');
insert into SearchDataTags values ('v2 25', 'radar', 'v2');
insert into SearchDataTags values ('v2 25', 'overview', 'v2');
insert into SearchDataTags values ('v2 25', 'hostage', 'v2');
insert into SearchDataTags values ('v2 25', 'bomb', 'v2');
insert into SearchDataTags values ('v2 26', '26', 'v2');
insert into SearchDataTags values ('v2 26', 'blendmodulate', 'v2');
insert into SearchDataTags values ('v2 26', 'blend', 'v2');
insert into SearchDataTags values ('bc1', '1', 'bc');
insert into SearchDataTags values ('bc1', 'installing', 'bc');
insert into SearchDataTags values ('bc1', 'understanding', 'bc');
insert into SearchDataTags values ('bc2', '2', 'bc');
insert into SearchDataTags values ('bc2', 'room', 'bc');
insert into SearchDataTags values ('bc2', 'skybox', 'bc');
insert into SearchDataTags values ('bc3', '3', 'bc');
insert into SearchDataTags values ('bc3', 'vertex', 'bc');
insert into SearchDataTags values ('bc3', 'shaping', 'bc');
insert into SearchDataTags values ('bc3', 'clipping', 'bc');
insert into SearchDataTags values ('bc4', '4', 'bc');
insert into SearchDataTags values ('bc4', 'intro', 'bc');
insert into SearchDataTags values ('bc4', 'introduction', 'bc');
insert into SearchDataTags values ('bc4', 'design', 'bc');
insert into SearchDataTags values ('bc4', 'layout', 'bc');
insert into SearchDataTags values ('bc5', '5', 'bc');
insert into SearchDataTags values ('bc5', 'testing', 'bc');
insert into SearchDataTags values ('bc5', 'layout', 'bc');
insert into SearchDataTags values ('bc5', 'design', 'bc');
insert into SearchDataTags values ('bc6', '6', 'bc');
insert into SearchDataTags values ('bc6', 'prop', 'bc');
insert into SearchDataTags values ('bc6', 'static', 'bc');
insert into SearchDataTags values ('bc6', 'prop_static', 'bc');
insert into SearchDataTags values ('bc6', 'prop_dynamic', 'bc');
insert into SearchDataTags values ('bc6', 'prop_physics', 'bc');
insert into SearchDataTags values ('bc7', '7', 'bc');
insert into SearchDataTags values ('bc7', 'optimization', 'bc');
insert into SearchDataTags values ('bc7', 'func_detail', 'bc');
insert into SearchDataTags values ('bc8', '8', 'bc');
insert into SearchDataTags values ('bc8', 'mode', 'bc');
insert into SearchDataTags values ('bc8', 'radar', 'bc');
insert into SearchDataTags values ('bc8', 'overview', 'bc');
insert into SearchDataTags values ('bc9', '9', 'bc');
insert into SearchDataTags values ('bc9', 'texture', 'bc');
insert into SearchDataTags values ('bc9', 'texturing', 'bc');
insert into SearchDataTags values ('bc10', '10', 'bc');
insert into SearchDataTags values ('bc10', 'displacement', 'bc');
insert into SearchDataTags values ('bc10', 'displacements', 'bc');
insert into SearchDataTags values ('bc10', 'sew', 'bc');
insert into SearchDataTags values ('bc11', '11', 'bc');
insert into SearchDataTags values ('bc11', 'lighting', 'bc');
insert into SearchDataTags values ('bc11', 'light', 'bc');
insert into SearchDataTags values ('bc11', 'light_spot', 'bc');
insert into SearchDataTags values ('bc12', '12', 'bc');
insert into SearchDataTags values ('bc12', 'lighting', 'bc');
insert into SearchDataTags values ('bc12', 'light', 'bc');
insert into SearchDataTags values ('bc12', 'sprite', 'bc');
insert into SearchDataTags values ('bc12', 'lightmap', 'bc');
insert into SearchDataTags values ('bc13', '13', 'bc');
insert into SearchDataTags values ('bc13', 'lighting', 'bc');
insert into SearchDataTags values ('bc13', 'light', 'bc');
insert into SearchDataTags values ('bc13', 'hdr', 'bc');
insert into SearchDataTags values ('bc13', 'smoothing', 'bc');
insert into SearchDataTags values ('bc14', '14', 'bc');
insert into SearchDataTags values ('bc14', 'doors', 'bc');
insert into SearchDataTags values ('bc14', 'chicken', 'bc');
insert into SearchDataTags values ('bc14', 'ladder', 'bc');
insert into SearchDataTags values ('bc15', '15', 'bc');
insert into SearchDataTags values ('bc15', 'skybox', 'bc');
insert into SearchDataTags values ('bc15', 'fog', 'bc');
insert into SearchDataTags values ('bc16', '16', 'bc');
insert into SearchDataTags values ('bc16', 'texture', 'bc');
insert into SearchDataTags values ('bc16', 'cubemap', 'bc');
insert into SearchDataTags values ('bc16', 'reflection', 'bc');
insert into SearchDataTags values ('bc16', 'envmap', 'bc');
insert into SearchDataTags values ('bc16', 'env_cubemap', 'bc');
insert into SearchDataTags values ('bc17', '17', 'bc');
insert into SearchDataTags values ('bc17', 'decal', 'bc');
insert into SearchDataTags values ('bc17', 'overlay', 'bc');
insert into SearchDataTags values ('bc17', 'detail', 'bc');
insert into SearchDataTags values ('bc18', '18', 'bc');
insert into SearchDataTags values ('bc18', 'sound', 'bc');
insert into SearchDataTags values ('bc18', 'soundscape', 'bc');
insert into SearchDataTags values ('bc19', '19', 'bc');
insert into SearchDataTags values ('bc19', 'optimization', 'bc');
insert into SearchDataTags values ('bc19', 'hint', 'bc');
insert into SearchDataTags values ('bc19', 'skip', 'bc');
insert into SearchDataTags values ('bc19', 'areaportal', 'bc');
insert into SearchDataTags values ('bc20', '20', 'bc');
insert into SearchDataTags values ('bc20', 'optimization', 'bc');
insert into SearchDataTags values ('bc20', 'hint', 'bc');
insert into SearchDataTags values ('bc20', 'skip', 'bc');
insert into SearchDataTags values ('bc20', 'areaportal', 'bc');
insert into SearchDataTags values ('bc21', '21', 'bc');
insert into SearchDataTags values ('bc21', 'publishing', 'bc');
insert into SearchDataTags values ('bc21', 'finalizing', 'bc');
insert into SearchDataTags values ('bc21', 'upload', 'bc');
insert into SearchDataTags values ('bc21', 'finish', 'bc');
insert into SearchDataTags values ('3ds mfp1', 'mfp1', '3ds');
insert into SearchDataTags values ('3ds mfp1', 'mfp', '3ds');
insert into SearchDataTags values ('3ds mfp1', 'first', '3ds');
insert into SearchDataTags values ('3ds mfp1', 'prop', '3ds');
insert into SearchDataTags values ('3ds mfp1', 'modeling', '3ds');
insert into SearchDataTags values ('3ds mfp1', '3dsmax', '3ds');
insert into SearchDataTags values ('3ds mfp2', 'mfp2', '3ds');
insert into SearchDataTags values ('3ds mfp2', 'mfp', '3ds');
insert into SearchDataTags values ('3ds mfp2', 'texturing', '3ds');
insert into SearchDataTags values ('3ds mfp2', 'substance', '3ds');
insert into SearchDataTags values ('3ds mfp3', 'mfp3', '3ds');
insert into SearchDataTags values ('3ds mfp3', 'mfp', '3ds');
insert into SearchDataTags values ('3ds mfp3', 'exporting', '3ds');
insert into SearchDataTags values ('3ds mfp3', 'wallworm', '3ds');
insert into SearchDataTags values ('3ds mfp3', 'wwmt', '3ds');
insert into SearchDataTags values ('3ds 1', '1', '3ds');
insert into SearchDataTags values ('3ds 1', 'introduction', '3ds');
insert into SearchDataTags values ('3ds 1', '3dsmax', '3ds');
insert into SearchDataTags values ('3ds 2', '2', '3ds');
insert into SearchDataTags values ('3ds 2', 'geometry', '3ds');
insert into SearchDataTags values ('3ds 2', 'primitives', '3ds');
insert into SearchDataTags values ('3ds 3', '3', '3ds');
insert into SearchDataTags values ('3ds 3', 'splines', '3ds');
insert into SearchDataTags values ('3ds 3', 'spline', '3ds');
insert into SearchDataTags values ('3ds 4', '4', '3ds');
insert into SearchDataTags values ('3ds 4', 'model', '3ds');
insert into SearchDataTags values ('3ds 4', 'basic', '3ds');
insert into SearchDataTags values ('3ds 4', 'lamp', '3ds');
insert into SearchDataTags values ('3ds 5', '5', '3ds');
insert into SearchDataTags values ('3ds 5', 'uvw', '3ds');
insert into SearchDataTags values ('3ds 5', 'unwrap', '3ds');
insert into SearchDataTags values ('3ds gen1', 'wwmt', '3ds');
insert into SearchDataTags values ('3ds gen1', 'wallworm', '3ds');
insert into SearchDataTags values ('3ds gen1', 'install', '3ds');
insert into SearchDataTags values ('3ds gen2', 'export', '3ds');
insert into SearchDataTags values ('3ds gen2', 'wwmt', '3ds');
insert into SearchDataTags values ('3ds gen2', 'wallworm', '3ds');
insert into SearchDataTags values ('3ds gen2', 'prop_static', '3ds');
insert into SearchDataTags values ('3ds gen2', 'static', '3ds');
insert into SearchDataTags values ('written1', '1', 'written');
insert into SearchDataTags values ('written1', 'lighting', 'written');
insert into SearchDataTags values ('written1', 'light', 'written');
insert into SearchDataTags values ('written2', '2', 'written');
insert into SearchDataTags values ('written2', 'finalizing', 'written');
insert into SearchDataTags values ('written2', 'release', 'written');
insert into SearchDataTags values ('written2', 'upload', 'written');
insert into SearchDataTags values ('written2', 'ship', 'written');
insert into SearchDataTags values ('written3', '3', 'written');
insert into SearchDataTags values ('written3', 'workflow', 'written');
insert into SearchDataTags values ('written3', 'process', 'written');
insert into SearchDataTags values ('written4', '4', 'written');
insert into SearchDataTags values ('written4', 'sound', 'written');
insert into SearchDataTags values ('written4', 'soundscript', 'written');
insert into SearchDataTags values ('written5', '5', 'written');
insert into SearchDataTags values ('written5', 'sprites', 'written');
insert into SearchDataTags values ('written5', 'worldspawn', 'written');
insert into SearchDataTags values ('written5', 'detail sprites', 'written');
insert into SearchDataTags values ('written6', '6', 'written');
insert into SearchDataTags values ('written6', 'hotkey', 'written');
insert into SearchDataTags values ('written7', '7', 'written');
insert into SearchDataTags values ('written7', 'cloud', 'written');
insert into SearchDataTags values ('written7', 'skybox', 'written');
insert into SearchDataTags values ('written8', '8', 'written');
insert into SearchDataTags values ('written8', 'protect', 'written');
insert into SearchDataTags values ('written8', 'decompiling', 'written');
insert into SearchDataTags values ('written9', '9', 'written');
insert into SearchDataTags values ('written9', 'process', 'written');
insert into SearchDataTags values ('written9', 'workflow', 'written');
insert into SearchDataTags values ('written10', '10', 'written');
insert into SearchDataTags values ('written10', 'vide', 'written');
insert into SearchDataTags values ('written10', 'pack', 'written');
insert into SearchDataTags values ('written11', '11', 'written');
insert into SearchDataTags values ('written11', 'screenshot', 'written');
insert into SearchDataTags values ('written11', 'image', 'written');
insert into SearchDataTags values ('written12', '12', 'written');
insert into SearchDataTags values ('written12', 'propper', 'written');
insert into SearchDataTags values ('written12', 'steampipe', 'written');
insert into SearchDataTags values ('written13', '13', 'written');
insert into SearchDataTags values ('written13', 'custom', 'written');
insert into SearchDataTags values ('written13', 'content', 'written');
insert into SearchDataTags values ('written13', 'contamination', 'written');
insert into SearchDataTags values ('written14', '14', 'written');
insert into SearchDataTags values ('written14', 'port', 'written');
insert into SearchDataTags values ('written14', 'porting', 'written');
insert into SearchDataTags values ('written14', 'content', 'written');
insert into SearchDataTags values ('written15', '15', 'written');
insert into SearchDataTags values ('written15', 'remote', 'written');
insert into SearchDataTags values ('written15', 'compile', 'written');
insert into SearchDataTags values ('written15', 'compiling', 'written');
insert into SearchDataTags values ('ht1', '1', 'ht');
insert into SearchDataTags values ('ht1', 'cubemap', 'ht');
insert into SearchDataTags values ('ht2', '2', 'ht');
insert into SearchDataTags values ('ht2', 'vertex', 'ht');
insert into SearchDataTags values ('ht2', 'shifting', 'ht');
insert into SearchDataTags values ('ht3', '3', 'ht');
insert into SearchDataTags values ('ht3', 'displacement', 'ht');
insert into SearchDataTags values ('ht3', 'edge', 'ht');
insert into SearchDataTags values ('ht3', 'shadow', 'ht');
insert into SearchDataTags values ('tips', 'tips', 'ht');
insert into SearchDataTags values ('tips', 'tricks', 'ht');
insert into SearchDataTags values ('legacy1', '1', 'lg');
insert into SearchDataTags values ('legacy1', 'started', 'lg');
insert into SearchDataTags values ('legacy1', 'beginning', 'lg');
insert into SearchDataTags values ('legacy2', '2', 'lg');
insert into SearchDataTags values ('legacy2', 'prop', 'lg');
insert into SearchDataTags values ('legacy2', 'prop_static', 'lg');
insert into SearchDataTags values ('legacy2', 'prop_dynamic', 'lg');
insert into SearchDataTags values ('legacy3', '3', 'lg');
insert into SearchDataTags values ('legacy3', 'lighting', 'lg');
insert into SearchDataTags values ('legacy3', 'light', 'lg');
insert into SearchDataTags values ('legacy3', 'light_spot', 'lg');
insert into SearchDataTags values ('legacy4', '4', 'lg');
insert into SearchDataTags values ('legacy4', 'door', 'lg');
insert into SearchDataTags values ('legacy4', 'buyzone', 'lg');
insert into SearchDataTags values ('legacy5', '5', 'lg');
insert into SearchDataTags values ('legacy5', 'water', 'lg');
insert into SearchDataTags values ('legacy5', 'displacement', 'lg');
insert into SearchDataTags values ('legacy6', '6', 'lg');
insert into SearchDataTags values ('legacy6', 'elevator', 'lg');
insert into SearchDataTags values ('legacy7', '7', 'lg');
insert into SearchDataTags values ('legacy7', 'deathrun', 'lg');
insert into SearchDataTags values ('legacy7', 'input', 'lg');
insert into SearchDataTags values ('legacy7', 'output', 'lg');
insert into SearchDataTags values ('legacy7', 'io', 'lg');
insert into SearchDataTags values ('legacy8', '8', 'lg');
insert into SearchDataTags values ('legacy8', 'train', 'lg');
insert into SearchDataTags values ('legacy8', 'laser', 'lg');
insert into SearchDataTags values ('legacy8', 'parent', 'lg');
insert into SearchDataTags values ('legacy8', 'parenting', 'lg');
insert into SearchDataTags values ('legacy9', '9', 'lg');
insert into SearchDataTags values ('legacy9', 'texture', 'lg');
insert into SearchDataTags values ('legacy9', 'custom', 'lg');
insert into SearchDataTags values ('legacy9', 'normal', 'lg');
insert into SearchDataTags values ('legacy10', '10', 'lg');
insert into SearchDataTags values ('legacy10', 'pakrat', 'lg');
insert into SearchDataTags values ('legacy10', 'vmex', 'lg');
insert into SearchDataTags values ('legacy11', '11', 'lg');
insert into SearchDataTags values ('legacy11', 'turret', 'lg');
insert into SearchDataTags values ('legacy11', 'cutting', 'lg');
insert into SearchDataTags values ('legacy11', 'angle', 'lg');
insert into SearchDataTags values ('legacy12', '12', 'lg');
insert into SearchDataTags values ('legacy12', 'point_template', 'lg');
insert into SearchDataTags values ('legacy12', 'template', 'lg');
insert into SearchDataTags values ('legacy12', 'spawn', 'lg');
insert into SearchDataTags values ('legacy13', '13', 'lg');
insert into SearchDataTags values ('legacy13', 'keypad', 'lg');
insert into SearchDataTags values ('legacy13', 'password', 'lg');
insert into SearchDataTags values ('legacy14', '14', 'lg');
insert into SearchDataTags values ('legacy14', 'textures', 'lg');
insert into SearchDataTags values ('legacy14', 'translucent', 'lg');
insert into SearchDataTags values ('legacy14', 'transparent', 'lg');
insert into SearchDataTags values ('legacy15', '15', 'lg');
insert into SearchDataTags values ('legacy15', 'custom', 'lg');
insert into SearchDataTags values ('legacy15', 'content', 'lg');
insert into SearchDataTags values ('legacy16', '16', 'lg');
insert into SearchDataTags values ('legacy16', 'trigger_teleport', 'lg');
insert into SearchDataTags values ('legacy16', 'teleport', 'lg');
insert into SearchDataTags values ('legacy16', 'hurt', 'lg');
insert into SearchDataTags values ('legacy16', 'trigger_hurt', 'lg');
insert into SearchDataTags values ('legacy16', 'heal', 'lg');
insert into SearchDataTags values ('legacy17', '17', 'lg');
insert into SearchDataTags values ('legacy17', 'afk', 'lg');
insert into SearchDataTags values ('legacy17', 'point_servercommand', 'lg');
insert into SearchDataTags values ('legacy18', '18', 'lg');
insert into SearchDataTags values ('legacy18', 'cell', 'lg');
insert into SearchDataTags values ('legacy18', 'shading', 'lg');
insert into SearchDataTags values ('legacy19', '19', 'lg');
insert into SearchDataTags values ('legacy19', 'math_counter', 'lg');
insert into SearchDataTags values ('legacy19', 'counter', 'lg');
insert into SearchDataTags values ('legacy19', 'func_breakable', 'lg');
insert into SearchDataTags values ('legacy19', 'breakable', 'lg');
insert into SearchDataTags values ('legacy19', 'logic_case', 'lg');
insert into SearchDataTags values ('legacy20', '20', 'lg');
insert into SearchDataTags values ('legacy20', 'demo', 'lg');
insert into SearchDataTags values ('legacy20', 'smoothing', 'lg');
insert into SearchDataTags values ('legacy21', '21', 'lg');
insert into SearchDataTags values ('legacy21', 'optimization', 'lg');
insert into SearchDataTags values ('legacy21', 'hint', 'lg');
insert into SearchDataTags values ('legacy21', 'skip', 'lg');
insert into SearchDataTags values ('legacy21', 'areaportal', 'lg');
insert into SearchDataTags values ('legacy22', '22', 'lg');
insert into SearchDataTags values ('legacy22', 'waterfall', 'lg');
insert into SearchDataTags values ('legacy23', '23', 'lg');
insert into SearchDataTags values ('legacy23', 'stairs', 'lg');
insert into SearchDataTags values ('legacy23', 'arches', 'lg');
insert into SearchDataTags values ('legacy23', 'primitives', 'lg');
insert into SearchDataTags values ('legacy24', '24', 'lg');
insert into SearchDataTags values ('legacy24', 'propper', 'lg');
insert into SearchDataTags values ('legacy25', '25', 'lg');
insert into SearchDataTags values ('legacy25', 'camera', 'lg');
insert into SearchDataTags values ('legacy25', 'rt', 'lg');
insert into SearchDataTags values ('legacy25', '_rt_camera', 'lg');
insert into SearchDataTags values ('legacy26', '26', 'lg');
insert into SearchDataTags values ('legacy26', 'refract', 'lg');
insert into SearchDataTags values ('legacy26', 'textures', 'lg');
insert into SearchDataTags values ('legacy27', '27', 'lg');
insert into SearchDataTags values ('legacy27', 'sound', 'lg');
insert into SearchDataTags values ('legacy27', 'ambient_generic', 'lg');
insert into SearchDataTags values ('legacy28', '28', 'lg');
insert into SearchDataTags values ('legacy28', 'compiling', 'lg');
insert into SearchDataTags values ('legacy28', 'vrad', 'lg');
insert into SearchDataTags values ('legacy28', 'vvis', 'lg');
insert into SearchDataTags values ('legacy28', 'vbsp', 'lg');
insert into SearchDataTags values ('legacy29', '29', 'lg');
insert into SearchDataTags values ('legacy29', 'ragdoll', 'lg');
insert into SearchDataTags values ('legacy29', 'prop_ragdoll', 'lg');
insert into SearchDataTags values ('legacy30', '30', 'lg');
insert into SearchDataTags values ('legacy30', 'skybox', 'lg');
insert into SearchDataTags values ('legacy30', 'sky_camera', 'lg');
insert into SearchDataTags values ('legacy31', '31', 'lg');
insert into SearchDataTags values ('legacy31', 'menu', 'lg');
insert into SearchDataTags values ('legacy32', '32', 'lg');
insert into SearchDataTags values ('legacy32', 'filter', 'lg');
insert into SearchDataTags values ('legacy32', 'filter_activator_team', 'lg');
insert into SearchDataTags values ('legacy32', 'filter_activator_damage', 'lg');
insert into SearchDataTags values ('legacy32', 'filters', 'lg');
insert into SearchDataTags values ('legacy33', '33', 'lg');
insert into SearchDataTags values ('legacy33', 'intput', 'lg');
insert into SearchDataTags values ('legacy33', 'output', 'lg');
insert into SearchDataTags values ('legacy33', 'io', 'lg');
insert into SearchDataTags values ('legacy34', '34', 'lg');
insert into SearchDataTags values ('legacy34', 'hole', 'lg');
insert into SearchDataTags values ('legacy34', 'displacement', 'lg');
insert into SearchDataTags values ('legacy35', '35', 'lg');
insert into SearchDataTags values ('legacy36', '36', 'lg');
insert into SearchDataTags values ('legacy36', 'timer', 'lg');
insert into SearchDataTags values ('legacy36', 'logic_timer', 'lg');
insert into SearchDataTags values ('legacy37', '37', 'lg');
insert into SearchDataTags values ('legacy37', 'orange', 'lg');
insert into SearchDataTags values ('legacy37', 'box', 'lg');
insert into SearchDataTags values ('legacy37', 'features', 'lg');
insert into SearchDataTags values ('legacy38', '38', 'lg');
insert into SearchDataTags values ('legacy38', 'graphics', 'lg');
insert into SearchDataTags values ('legacy39', '39', 'lg');
insert into SearchDataTags values ('legacy39', 'visgroup', 'lg');
insert into SearchDataTags values ('legacy40', '40', 'lg');
insert into SearchDataTags values ('legacy40', 'displacement', 'lg');
insert into SearchDataTags values ('legacy40', 'sew', 'lg');
insert into SearchDataTags values ('legacy41', '41', 'lg');
insert into SearchDataTags values ('legacy41', 'radar', 'lg');
insert into SearchDataTags values ('legacy41', 'overview', 'lg');
insert into SearchDataTags values ('legacy42', '42', 'lg');
insert into SearchDataTags values ('legacy42', 'lights.rad', 'lg');
insert into SearchDataTags values ('legacy42', 'rad', 'lg');
insert into SearchDataTags values ('legacy43', '43', 'lg');
insert into SearchDataTags values ('legacy43', 'water', 'lg');
insert into SearchDataTags values ('legacy43', 'dynamic', 'lg');
insert into SearchDataTags values ('legacy44', '44', 'lg');
insert into SearchDataTags values ('legacy44', 'map', 'lg');
insert into SearchDataTags values ('legacy44', 'background', 'lg');
insert into SearchDataTags values ('legacy45', '45', 'lg');
insert into SearchDataTags values ('legacy45', 'water', 'lg');
insert into SearchDataTags values ('legacy46', '46', 'lg');
insert into SearchDataTags values ('legacy46', 'weather', 'lg');
insert into SearchDataTags values ('legacy46', 'effect', 'lg');
insert into SearchDataTags values ('legacy47', '47', 'lg');
insert into SearchDataTags values ('legacy47', 'sprites', 'lg');
insert into SearchDataTags values ('legacy47', 'worldspawn', 'lg');
insert into SearchDataTags values ('legacy47', 'detail.vbsp', 'lg');
insert into SearchDataTags values ('legacy48', '48', 'lg');
insert into SearchDataTags values ('legacy48', 'speed', 'lg');
insert into SearchDataTags values ('legacy48', 'gravity', 'lg');
insert into SearchDataTags values ('legacy49', '49', 'lg');
insert into SearchDataTags values ('legacy49', 'portal', 'lg');
insert into SearchDataTags values ('legacy49', 'stairs', 'lg');
insert into SearchDataTags values ('legacy50', '50', 'lg');
insert into SearchDataTags values ('legacy50', 'skybox', 'lg');
insert into SearchDataTags values ('legacy50', 'sky_camera', 'lg');
insert into SearchDataTags values ('legacy51', '51', 'lg');
insert into SearchDataTags values ('legacy51', 'cable', 'lg');
insert into SearchDataTags values ('legacy51', 'rope', 'lg');
insert into SearchDataTags values ('legacy51', 'keyfram_rope', 'lg');
insert into SearchDataTags values ('legacy51', 'move_rope', 'lg');
insert into SearchDataTags values ('legacy52', '52', 'lg');
insert into SearchDataTags values ('legacy52', 'street', 'lg');
insert into SearchDataTags values ('legacy52', 'light', 'lg');
insert into SearchDataTags values ('legacy53', '53', 'lg');
insert into SearchDataTags values ('legacy53', 'cubemap', 'lg');
insert into SearchDataTags values ('legacy53', 'env_cubemap', 'lg');
insert into SearchDataTags values ('legacy53', 'reflections', 'lg');
insert into SearchDataTags values ('legacy54', '54', 'lg');
insert into SearchDataTags values ('legacy54', 'prop', 'lg');
insert into SearchDataTags values ('legacy54', 'prop_static', 'lg');
insert into SearchDataTags values ('legacy54', 'prop_dynamic', 'lg');
insert into SearchDataTags values ('legacy55', '55', 'lg');
insert into SearchDataTags values ('legacy55', 'hdr', 'lg');
insert into SearchDataTags values ('legacy55', 'light', 'lg');
insert into SearchDataTags values ('legacy55', 'lighting', 'lg');
insert into SearchDataTags values ('legacy56', '56', 'lg');
insert into SearchDataTags values ('legacy56', 'sound', 'lg');
insert into SearchDataTags values ('legacy56', 'soundscape', 'lg');
insert into SearchDataTags values ('legacy56', 'env_soundscape', 'lg');
insert into SearchDataTags values ('legacy57', '57', 'lg');
insert into SearchDataTags values ('legacy57', 'relay', 'lg');
insert into SearchDataTags values ('legacy57', 'logic_relay', 'lg');
insert into SearchDataTags values ('legacy57', 'toggle', 'lg');
insert into SearchDataTags values ('legacy57', 'switch', 'lg');
insert into SearchDataTags values ('legacy58', '58', 'lg');
insert into SearchDataTags values ('legacy58', 'game_ui', 'lg');
insert into SearchDataTags values ('legacy59', '59', 'lg');
insert into SearchDataTags values ('legacy59', 'path_track', 'lg');
insert into SearchDataTags values ('legacy59', 'func_tracktain', 'lg');
insert into SearchDataTags values ('legacy60', '60', 'lg');
insert into SearchDataTags values ('legacy60', 'console', 'lg');
insert into SearchDataTags values ('legacy60', 'chat', 'lg');
insert into SearchDataTags values ('legacy60', 'point_servercommand', 'lg');
insert into SearchDataTags values ('legacy61', '61', 'lg');
insert into SearchDataTags values ('legacy61', 'demo', 'lg');
insert into SearchDataTags values ('legacy61', 'smoothing', 'lg');
insert into SearchDataTags values ('legacy62', '62', 'lg');
insert into SearchDataTags values ('legacy62', 'jailbreak', 'lg');
insert into SearchDataTags values ('legacy62', 'jb', 'lg');
insert into SearchDataTags values ('legacy63', '63', 'lg');
insert into SearchDataTags values ('legacy63', 'game_end', 'lg');
insert into SearchDataTags values ('legacy63', 'score', 'lg');
insert into SearchDataTags values ('legacy64', '64', 'lg');
insert into SearchDataTags values ('legacy64', 'trigger_look', 'lg');
insert into SearchDataTags values ('legacy64', 'look', 'lg');
insert into SearchDataTags values ('legacy65', '65', 'lg');
insert into SearchDataTags values ('legacy65', 'magnet', 'lg');
insert into SearchDataTags values ('legacy65', 'phys_magnet', 'lg');
insert into SearchDataTags values ('legacy66', '66', 'lg');
insert into SearchDataTags values ('legacy66', 'color', 'lg');
insert into SearchDataTags values ('legacy66', 'correction', 'lg');
insert into SearchDataTags values ('legacy67', '67', 'lg');
insert into SearchDataTags values ('legacy67', 'roads', 'lg');
insert into SearchDataTags values ('legacy68', '68', 'lg');
insert into SearchDataTags values ('legacy68', 'func_viscluster', 'lg');
insert into SearchDataTags values ('legacy68', 'viscluster', 'lg');
insert into SearchDataTags values ('legacy69', '69', 'lg');
insert into SearchDataTags values ('legacy69', 'keycard', 'lg');
insert into SearchDataTags values ('legacy70', '70', 'lg');
insert into SearchDataTags values ('legacy70', 'change', 'lg');
insert into SearchDataTags values ('legacy70', 'route', 'lg');
insert into SearchDataTags values ('legacy70', 'logic_auto', 'lg');
insert into SearchDataTags values ('legacy70', 'env_global', 'lg');
insert into SearchDataTags values ('legacy71', '71', 'lg');
insert into SearchDataTags values ('legacy71', 'stair', 'lg');
insert into SearchDataTags values ('legacy72', '72', 'lg');
insert into SearchDataTags values ('legacy72', 'displacement', 'lg');
insert into SearchDataTags values ('legacy72', 'cave', 'lg');
insert into SearchDataTags values ('legacy73', '73', 'lg');
insert into SearchDataTags values ('legacy73', 'info_particle_system', 'lg');
insert into SearchDataTags values ('legacy73', 'particle', 'lg');
insert into SearchDataTags values ('legacy73', 'system', 'lg');
insert into SearchDataTags values ('legacy74', '74', 'lg');
insert into SearchDataTags values ('legacy74', 'lighting', 'lg');
insert into SearchDataTags values ('legacy74', 'light', 'lg');
insert into SearchDataTags values ('legacy74', 'hdr', 'lg');
insert into SearchDataTags values ('legacy75', '75', 'lg');
insert into SearchDataTags values ('legacy75', 'text', 'lg');
insert into SearchDataTags values ('legacy76', '76', 'lg');
insert into SearchDataTags values ('legacy76', 'invisible', 'lg');
insert into SearchDataTags values ('legacy76', 'player', 'lg');
insert into SearchDataTags values ('legacy77', '77', 'lg');
insert into SearchDataTags values ('legacy77', 'phys_ballsocket', 'lg');
insert into SearchDataTags values ('legacy77', 'swing', 'lg');
insert into SearchDataTags values ('legacy78', '78', 'lg');
insert into SearchDataTags values ('legacy78', 'elevator', 'lg');
insert into SearchDataTags values ('legacy79', '79', 'lg');
insert into SearchDataTags values ('legacy79', 'mirror', 'lg');
insert into SearchDataTags values ('legacy79', 'func_reflective_glass', 'lg');
insert into SearchDataTags values ('legacy80', '80', 'lg');
insert into SearchDataTags values ('legacy80', 'env_explosion', 'lg');
insert into SearchDataTags values ('legacy80', 'wall', 'lg');
insert into SearchDataTags values ('legacy80', 'func_physbox', 'lg');
insert into SearchDataTags values ('legacy81', '81', 'lg');
insert into SearchDataTags values ('legacy81', 'logic_measuremovement', 'lg');
insert into SearchDataTags values ('legacy81', 'turret', 'lg');
insert into SearchDataTags values ('legacy81', 'mouse', 'lg');
insert into SearchDataTags values ('legacy82', '82', 'lg');
insert into SearchDataTags values ('legacy82', 'teleport', 'lg');
insert into SearchDataTags values ('legacy82', 'trigger_teleport', 'lg');
insert into SearchDataTags values ('legacy83', '83', 'lg');
insert into SearchDataTags values ('legacy83', 'keypad', 'lg');
insert into SearchDataTags values ('legacy83', 'random', 'lg');
insert into SearchDataTags values ('legacy84', '84', 'lg');
insert into SearchDataTags values ('legacy84', 'filter_activator_team', 'lg');
insert into SearchDataTags values ('legacy84', 'button', 'lg');
insert into SearchDataTags values ('legacy85', '85', 'lg');
insert into SearchDataTags values ('legacy85', 'portal', 'lg');
insert into SearchDataTags values ('legacy85', 'parent', 'lg');
insert into SearchDataTags values ('legacy85', 'attachment', 'lg');
insert into SearchDataTags values ('legacy86', '86', 'lg');
insert into SearchDataTags values ('legacy86', 'instance', 'lg');
insert into SearchDataTags values ('legacy86', 'func_instance', 'lg');
insert into SearchDataTags values ('legacy87', '87', 'lg');
insert into SearchDataTags values ('legacy87', 'portal', 'lg');
insert into SearchDataTags values ('legacy87', 'button', 'lg');
insert into SearchDataTags values ('legacy87', 'indicator', 'lg');
insert into SearchDataTags values ('legacy88', '88', 'lg');
insert into SearchDataTags values ('legacy88', 'info', 'lg');
insert into SearchDataTags values ('legacy88', 'signs', 'lg');
insert into SearchDataTags values ('legacy89', '89', 'lg');
insert into SearchDataTags values ('legacy89', 'world', 'lg');
insert into SearchDataTags values ('legacy89', 'portals', 'lg');
insert into SearchDataTags values ('legacy89', 'linked_portal_door', 'lg');
insert into SearchDataTags values ('legacy90', '90', 'lg');
insert into SearchDataTags values ('legacy90', 'portal', 'lg');
insert into SearchDataTags values ('legacy90', 'elevator', 'lg');
insert into SearchDataTags values ('legacy91', '91', 'lg');
insert into SearchDataTags values ('legacy91', 'looping', 'lg');
insert into SearchDataTags values ('legacy91', 'soundscape', 'lg');
insert into SearchDataTags values ('legacy92', '92', 'lg');
insert into SearchDataTags values ('legacy92', 'portals', 'lg');
insert into SearchDataTags values ('legacy92', 'prop_portal', 'lg');
insert into SearchDataTags values ('legacy93', '93', 'lg');
insert into SearchDataTags values ('legacy93', 'fog', 'lg');
insert into SearchDataTags values ('legacy93', 'env_fog_controller', 'lg');
insert into SearchDataTags values ('legacy93', 'fog_volume', 'lg');
insert into SearchDataTags values ('legacy94', '94', 'lg');
insert into SearchDataTags values ('legacy94', 'info_lighting', 'lg');
insert into SearchDataTags values ('legacy94', 'origin', 'lg');
insert into SearchDataTags values ('legacy95', '95', 'lg');
insert into SearchDataTags values ('legacy95', 'env_projectedtexture', 'lg');
insert into SearchDataTags values ('legacy95', 'dynamic', 'lg');
insert into SearchDataTags values ('legacy95', 'light', 'lg');
insert into SearchDataTags values ('legacy96', '96', 'lg');
insert into SearchDataTags values ('legacy96', 'water', 'lg');
insert into SearchDataTags values ('legacy96', 'river', 'lg');
insert into SearchDataTags values ('legacy96', 'rivers', 'lg');
insert into SearchDataTags values ('legacy97', '97', 'lg');
insert into SearchDataTags values ('legacy97', 'game_text', 'lg');
insert into SearchDataTags values ('legacy97', 'point_servercommand', 'lg');
insert into SearchDataTags values ('legacy97', 'env_hudhint', 'lg');
insert into SearchDataTags values ('legacy98', '98', 'lg');
insert into SearchDataTags values ('legacy98', 'break', 'lg');
insert into SearchDataTags values ('legacy98', 'light', 'lg');
insert into SearchDataTags values ('legacy99', '99', 'lg');
insert into SearchDataTags values ('legacy99', 'texture', 'lg');
insert into SearchDataTags values ('legacy99', 'justify', 'lg');
insert into SearchDataTags values ('legacy99', 'alignment', 'lg');
insert into SearchDataTags values ('legacy99', 'align', 'lg');
insert into SearchDataTags values ('legacy100', '100', 'lg');
insert into SearchDataTags values ('legacy100', 'particle', 'lg');
insert into SearchDataTags values ('legacy100', 'info_particle_system', 'lg');

insert into SearchDataResults
values (
    'v2 1',
    'https://www.tophattwaffle.com/introducing-hammer-tutorial-v2-series-1-launching-hammer-initial-configuration-making-a-room/');
insert into SearchDataResults
values (
    'v2 2',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-2-adding-a-sky-to-your-level/');
insert into SearchDataResults
values (
    'v2 3',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-3-getting-intimate-with-brush-tool-and-introduction-to-clipping-tool/');
insert into SearchDataResults
values (
    'v2 4',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-4-leaks/');
insert into SearchDataResults
values (
    'v2 5',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-5-introduction-to-props/');
insert into SearchDataResults
values (
    'v2 6',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-6-basic-optimization/');
insert into SearchDataResults
values (
    'v2 7',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-7-vertex-tool-primitive-prefab-creation/');
insert into SearchDataResults
values (
    'v2 8',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-8-complete-lighting-tutorial/');
insert into SearchDataResults
values (
    'v2 9',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-9-displacements/');
insert into SearchDataResults
values (
    'v2 10',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-10-adding-water-to-your-level/');
insert into SearchDataResults
values (
    'v2 11',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-11-adding-doors-to-your-level/');
insert into SearchDataResults
values (
    'v2 12',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-12-input-and-output-overview/');
insert into SearchDataResults
values (
    'v2 13',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-13-creating-custom-textures-normals-vmts-packing-a-levels-content/');
insert into SearchDataResults
values (
    'v2 14',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-14-env_cubemaps-static-reflections-reflective-textures/');
insert into SearchDataResults
values (
    'v2 15',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-15-creating-a-3d-skybox/');
insert into SearchDataResults
values (
    'v2 16',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-16-adding-fog-to-your-level/');
insert into SearchDataResults
values (
    'v2 17',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-17-color-correction/');
insert into SearchDataResults
values (
    'v2 18',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-18-func_movelinear-basic/');
insert into SearchDataResults
values (
    'v2 19',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-19-parenting-attachment-points/');
insert into SearchDataResults
values (
    'v2 20',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-20-func_rotating-making-fans-and-things-spin/');
insert into SearchDataResults
values (
    'v2 21',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-21-optimization/');
insert into SearchDataResults
values (
    'v2 22',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-22-complete-sound-implementation/');
insert into SearchDataResults
values (
    'v2 23',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-23-texture-manipulation/');
insert into SearchDataResults
values (
    'v2 24',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-24-infodecal-and-info_overlays/');
insert into SearchDataResults
values (
    'v2 25',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-25-radar-overviews/');
insert into SearchDataResults
values (
    'v2 26',
    'https://www.tophattwaffle.com/hammer-tutorial-v2-series-26-will-it-blend-blend-modulate/');
insert into SearchDataResults
values (
    'bc1',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-1-installing-and-understanding-the-sdk-tools/');
insert into SearchDataResults
values (
    'bc2',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-2-creating-our-first-room/');
insert into SearchDataResults
values (
    'bc3',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-3-shaping-brushes/');
insert into SearchDataResults
values (
    'bc4',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-4-intro-to-cs-level-design/');
insert into SearchDataResults
values (
    'bc5',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-5-building-testing-and-fixing-dev-level/');
insert into SearchDataResults
values (
    'bc6',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-6-introduction-to-props/');
insert into SearchDataResults
values (
    'bc7',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-7-introduction-to-optimization/');
insert into SearchDataResults
values (
    'bc8',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-8-game-modes-and-radars/');
insert into SearchDataResults
values (
    'bc9',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-9-texturing-your-level/');
insert into SearchDataResults
values (
    'bc10',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-10-displacements/');
insert into SearchDataResults
values (
    'bc11',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-11-lighting-part-1-basic-lighting/');
insert into SearchDataResults
values (
    'bc12',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-12-lighting-part-2-sprites-effects-lightmap-grid/');
insert into SearchDataResults
values (
    'bc13',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-13-lighting-part-3-advanced-lighting-hdr/');
insert into SearchDataResults
values (
    'bc14',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-14-doors-ladders-and-chickens/');
insert into SearchDataResults
values (
    'bc15',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-15-3d-skybox-and-fog/');
insert into SearchDataResults
values (
    'bc16',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-16-custom-textures-cubemaps-and-reflections/');
insert into SearchDataResults
values (
    'bc17',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-17-decals-and-info_overlay/');
insert into SearchDataResults
values (
    'bc18',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-18-sounds/');
insert into SearchDataResults
values (
    'bc19',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-19-optimization-part-1/');
insert into SearchDataResults
values (
    'bc20',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-20-optimization-part-2/');
insert into SearchDataResults
values (
    'bc21',
    'https://www.tophattwaffle.com/csgo-level-design-boot-camp-day-21-finalizing-and-publishing/');
insert into SearchDataResults
values (
    '3ds mfp1',
    'https://www.tophattwaffle.com/create-your-own-csgo-props-my-first-source-prop-part-1-modeling/');
insert into SearchDataResults
values (
    '3ds mfp2',
    'https://www.tophattwaffle.com/create-your-own-csgo-props-my-first-source-prop-part-2-texturing/');
insert into SearchDataResults
values (
    '3ds mfp3',
    'https://www.tophattwaffle.com/create-your-own-csgo-props-my-first-source-prop-part-3-exporting/');
insert into SearchDataResults
values (
    '3ds 1',
    'https://www.tophattwaffle.com/3dsmax-tutorial-beginners-guide-1-introduction-to-max/');
insert into SearchDataResults
values (
    '3ds 2',
    'https://www.tophattwaffle.com/3dsmax-tutorial-beginners-guide-2-creating-and-editing-geometry/');
insert into SearchDataResults
values (
    '3ds 3',
    'https://www.tophattwaffle.com/3dsmax-tutorial-beginners-guide-3-creating-and-editing-splines/');
insert into SearchDataResults
values (
    '3ds 4',
    'https://www.tophattwaffle.com/3dsmax-tutorial-beginners-guide-4-creating-a-simple-model-lamp/');
insert into SearchDataResults
values (
    '3ds 5',
    'https://www.tophattwaffle.com/3dsmax-tutorial-beginners-guide-5-uvw-unwrap-modifier/');
insert into SearchDataResults
values (
    '3ds gen1',
    'https://www.tophattwaffle.com/3dsmax-tutorial-basic-install-of-wallworm-model-tools/');
insert into SearchDataResults
values (
    '3ds gen2',
    'https://www.tophattwaffle.com/3dsmax-tutorial-exporting-a-model-to-source-engine-prop_static/');
insert into SearchDataResults
values (
    'written1',
    'https://www.tophattwaffle.com/lighting-tips-tricks-and-hints/');
insert into SearchDataResults
values (
    'written2',
    'https://www.tophattwaffle.com/finalizing-your-level-before-release/');
insert into SearchDataResults
values (
    'written3',
    'https://www.tophattwaffle.com/mapping-workflow-keeping-your-sanity-for-the-extra-long-projects/');
insert into SearchDataResults
values (
    'written4',
    'https://www.tophattwaffle.com/creating-your-own-sound-scripts/');
insert into SearchDataResults
values (
    'written5',
    'https://www.tophattwaffle.com/detail-props-on-world-spawn-start-to-finish/');
insert into SearchDataResults
values ('written6', 'https://www.tophattwaffle.com/hammer-hot-key-list/');
insert into SearchDataResults
values (
    'written7',
    'https://www.tophattwaffle.com/custom-clouds-for-3d-sky-box-tutorial/');
insert into SearchDataResults
values (
    'written8',
    'https://www.tophattwaffle.com/protecting-a-map-from-decompiling/');
insert into SearchDataResults
values ('written9', 'https://www.tophattwaffle.com/my-level-design-process/');
insert into SearchDataResults
values (
    'written10',
    'https://www.tophattwaffle.com/packing-custom-content-using-vide-in-steampipe/');
insert into SearchDataResults
values (
    'written11',
    'https://www.tophattwaffle.com/taking-screenshots-the-proper-way-to-showcase-your-level/');
insert into SearchDataResults
values (
    'written12',
    'https://www.tophattwaffle.com/configuring-propper-for-steampipe/');
insert into SearchDataResults
values (
    'written13',
    'https://www.tophattwaffle.com/custom-content-without-contamination/');
insert into SearchDataResults
values (
    'written14',
    'https://www.tophattwaffle.com/porting-content-from-other-source-engine-games/');
insert into SearchDataResults
values (
    'written15',
    'https://www.tophattwaffle.com/configuring-remote-compiling/');
insert into SearchDataResults
values (
    'ht1',
    'https://www.tophattwaffle.com/hammer-trouble-shooting-1-css-cubemaps-wont-build/');
insert into SearchDataResults
values (
    'ht2',
    'https://www.tophattwaffle.com/hammer-trouble-shooting-2-vertex-points-moving-around-on-savecompile/');
insert into SearchDataResults
values (
    'ht3',
    'https://www.tophattwaffle.com/hammer-trouble-shooting-3-shadows-on-displacement-edges/');
insert into SearchDataResults
values ('tips', 'https://www.youtube.com/watch?v=47HR2jewQms');
insert into SearchDataResults
values (
    'legacy1',
    'https://www.tophattwaffle.com/hammer-tutorial-1-simple-things-remake/');
insert into SearchDataResults
values
    ('legacy2', 'https://www.tophattwaffle.com/hammer-tutorial-2-prop-basics/');
insert into SearchDataResults
values ('legacy3', 'https://www.tophattwaffle.com/hammer-tutorial-3-lighting/');
insert into SearchDataResults
values (
    'legacy4',
    'https://www.tophattwaffle.com/hammer-tutorial-4-de_cs_-buyzone-and-doors/');
insert into SearchDataResults
values (
    'legacy5',
    'https://www.tophattwaffle.com/hammer-tutorial-5-water-displacements-and-glass-oh-my/');
insert into SearchDataResults
values ('legacy6', 'https://www.tophattwaffle.com/hammer-tutorial-6-elevator/');
insert into SearchDataResults
values (
    'legacy7',
    'https://www.tophattwaffle.com/hammer-tutorial-7-deathrun-and-io-making/');
insert into SearchDataResults
values (
    'legacy8',
    'https://www.tophattwaffle.com/hammer-tutorial-8-laser-trails-and-parented-items/');
insert into SearchDataResults
values (
    'legacy9',
    'https://www.tophattwaffle.com/hammer-tutorial-9-custom-textures-and-normal-maps/');
insert into SearchDataResults
values (
    'legacy10',
    'https://www.tophattwaffle.com/hammer-tutorial-10-pakrat-and-vmex/');
insert into SearchDataResults
values (
    'legacy11',
    'https://www.tophattwaffle.com/hammer-tutorial-11-waffle-turret-and-cutting-to-a-angle/');
insert into SearchDataResults
values (
    'legacy12',
    'https://www.tophattwaffle.com/hammer-tutorial-12-spawing-props-and-other-things/');
insert into SearchDataResults
values (
    'legacy13',
    'https://www.tophattwaffle.com/hammer-tutorial-13-200-subscribers-special-keypads/');
insert into SearchDataResults
values (
    'legacy14',
    'https://www.tophattwaffle.com/hammer-tutorial-14-advanced-textures-transparent-textures/');
insert into SearchDataResults
values (
    'legacy15',
    'https://www.tophattwaffle.com/hammer-tutorial-15-custom-content/');
insert into SearchDataResults
values (
    'legacy16',
    'https://www.tophattwaffle.com/hammer-tutorial-16-teleport-hurt-heal-and-stripping-weapons/');
insert into SearchDataResults
values (
    'legacy17',
    'https://www.tophattwaffle.com/hammer-tutorial-17-making-a-afk-killer-and-firing-commands-to-a-server/');
insert into SearchDataResults
values (
    'legacy18',
    'https://www.tophattwaffle.com/hammer-tutorial-18-making-cell-shading-brush-work/');
insert into SearchDataResults
values (
    'legacy19',
    'https://www.tophattwaffle.com/hammer-tutorial-19-math_counter-func_breakable-and-logic_case-all-good-things-to-know/');
insert into SearchDataResults
values (
    'legacy20',
    'https://www.tophattwaffle.com/hammer-tutorial-20-demo-smoothing/');
insert into SearchDataResults
values (
    'legacy21',
    'https://www.tophattwaffle.com/hammer-tutorial-21-optimization/');
insert into SearchDataResults
values (
    'legacy22',
    'https://www.tophattwaffle.com/hammer-tutorial-22-waterfalls/');
insert into SearchDataResults
values (
    'legacy23',
    'https://www.tophattwaffle.com/hammer-tutorial-23-stairs-arches-and-more/');
insert into SearchDataResults
values
    ('legacy24', 'https://www.tophattwaffle.com/hammer-tutorial-24-propper/');
insert into SearchDataResults
values (
    'legacy25',
    'https://www.tophattwaffle.com/hammer-tutorial-25-_rt_camera/');
insert into SearchDataResults
values (
    'legacy26',
    'https://www.tophattwaffle.com/hammer-tutorial-26-refracting-images-advanced-textures/');
insert into SearchDataResults
values (
    'legacy27',
    'https://www.tophattwaffle.com/hammer-tutorial-27-importing-custom-sounds/');
insert into SearchDataResults
values
    ('legacy28', 'https://www.tophattwaffle.com/hammer-tutorial-28-compiling/');
insert into SearchDataResults
values (
    'legacy29',
    'https://www.tophattwaffle.com/hammer-tutorial-29-500th-subscriber-special-random-falling-ragdolls/');
insert into SearchDataResults
values
    ('legacy30', 'https://www.tophattwaffle.com/hammer-tutorial-30-3d-skybox/');
insert into SearchDataResults
values (
    'legacy31',
    'https://www.tophattwaffle.com/hammer-tutorial-31-custom-game-menu/');
insert into SearchDataResults
values (
    'legacy32',
    'https://www.tophattwaffle.com/hammer-tutorial-32-basic-filters/');
insert into SearchDataResults
values (
    'legacy33',
    'https://www.tophattwaffle.com/hammer-tutorial-33-inputs-and-outputs/');
insert into SearchDataResults
values (
    'legacy34',
    'https://www.tophattwaffle.com/hammer-tutorial-34-holes-in-the-walls/');
insert into SearchDataResults
values (
    'legacy35',
    'https://www.tophattwaffle.com/hammer-tutorial-35-adding-counter-strike-source-to-hammer/');
insert into SearchDataResults
values ('legacy36', 'https://www.tophattwaffle.com/hammer-tutorial-36-timer/');
insert into SearchDataResults
values (
    'legacy37',
    'https://www.tophattwaffle.com/hammer-tutorial-37-orange-box-hammer-whats-new/');
insert into SearchDataResults
values (
    'legacy38',
    'https://www.tophattwaffle.com/hammer-tutorial-38-changing-the-graphics-in-hammer/');
insert into SearchDataResults
values (
    'legacy39',
    'https://www.tophattwaffle.com/hammer-tutorial-39-visgrouping/');
insert into SearchDataResults
values (
    'legacy40',
    'https://www.tophattwaffle.com/hammer-tutorial-40-displacements/');
insert into SearchDataResults
values (
    'legacy41',
    'https://www.tophattwaffle.com/hammer-tutorial-41-custom-radar/');
insert into SearchDataResults
values (
    'legacy42',
    'https://www.tophattwaffle.com/hammer-tutorial-42-glowing-textures-lights-rad/');
insert into SearchDataResults
values (
    'legacy43',
    'https://www.tophattwaffle.com/hammer-tutorial-43-dynamic-water/');
insert into SearchDataResults
values (
    'legacy44',
    'https://www.tophattwaffle.com/hammer-tutorial-44-making-your-own-custom-map-background/');
insert into SearchDataResults
values (
    'legacy45',
    'https://www.tophattwaffle.com/hammer-tutorial-45-adding-water-the-correct-way/');
insert into SearchDataResults
values (
    'legacy46',
    'https://www.tophattwaffle.com/hammer-tutorial-46-weather-effects/');
insert into SearchDataResults
values (
    'legacy47',
    'https://www.tophattwaffle.com/hammer-tutorial-47-sprites-on-world-spawn-detail-vbsp/');
insert into SearchDataResults
values (
    'legacy48',
    'https://www.tophattwaffle.com/hammer-tutorial-48-modifying-speed-and-gravity/');
insert into SearchDataResults
values (
    'legacy49',
    'https://www.tophattwaffle.com/hammer-tutorial-49-portal-stairs/');
insert into SearchDataResults
values (
    'legacy50',
    'https://www.tophattwaffle.com/hammer-tutorial-50-d-perfectly-lining-up-3d-skyboxes/');
insert into SearchDataResults
values (
    'legacy51',
    'https://www.tophattwaffle.com/hammer-tutorial-51-cable-and-ropes/');
insert into SearchDataResults
values (
    'legacy52',
    'https://www.tophattwaffle.com/hammer-tutorial-52-street-light-1000th-sub/');
insert into SearchDataResults
values
    ('legacy53', 'https://www.tophattwaffle.com/hammer-tutorial-53-cubemaps/');
insert into SearchDataResults
values (
    'legacy54',
    'https://www.tophattwaffle.com/hammer-tutorial-54-advanced-props/');
insert into SearchDataResults
values (
    'legacy55',
    'https://www.tophattwaffle.com/hammer-tutorial-55-hdr-lighting/');
insert into SearchDataResults
values (
    'legacy56',
    'https://www.tophattwaffle.com/hammer-tutorial-56-soundscapes/');
insert into SearchDataResults
values (
    'legacy57',
    'https://www.tophattwaffle.com/hammer-tutorial-57-multi-toggle-button/');
insert into SearchDataResults
values
    ('legacy58', 'https://www.tophattwaffle.com/hammer-tutorial-58-game_ui/');
insert into SearchDataResults
values (
    'legacy59',
    'https://www.tophattwaffle.com/hammer-tutorial-59-path_track-and-func_tanktrain/');
insert into SearchDataResults
values (
    'legacy60',
    'https://www.tophattwaffle.com/hammer-tutorial-60-console-chat-console-commands/');
insert into SearchDataResults
values (
    'legacy61',
    'https://www.tophattwaffle.com/hammer-tutorial-61-demo-smoothing-in-css-2009-fixing-the-smooth-bug/');
insert into SearchDataResults
values (
    'legacy62',
    'https://www.tophattwaffle.com/hammer-tutorial-62-jailbreak-bare-min/');
insert into SearchDataResults
values (
    'legacy63',
    'https://www.tophattwaffle.com/hammer-tutorial-63-ending-a-level-adding-score/');
insert into SearchDataResults
values (
    'legacy64',
    'https://www.tophattwaffle.com/hammer-tutorial-64-trigger_look/');
insert into SearchDataResults
values (
    'legacy65',
    'https://www.tophattwaffle.com/hammer-tutorial-65-magnets-glock-totin-wcc/');
insert into SearchDataResults
values (
    'legacy66',
    'https://www.tophattwaffle.com/hammer-tutorial-66-color-correction/');
insert into SearchDataResults
values (
    'legacy67',
    'https://www.tophattwaffle.com/hammer-tutorial-67-making-roads-the-correct-way/');
insert into SearchDataResults
values (
    'legacy68',
    'https://www.tophattwaffle.com/hammer-tutorial-68-func_viscluster/');
insert into SearchDataResults
values (
    'legacy69',
    'https://www.tophattwaffle.com/hammer-tutorial-69-keycard-system/');
insert into SearchDataResults
values (
    'legacy70',
    'https://www.tophattwaffle.com/hammer-tutorial-70-change-in-next-round-css/');
insert into SearchDataResults
values (
    'legacy71',
    'https://www.tophattwaffle.com/hammer-tutorial-71-metal-stair-case/');
insert into SearchDataResults
values (
    'legacy72',
    'https://www.tophattwaffle.com/hammer-tutorial-72-displacement-cave/');
insert into SearchDataResults
values (
    'legacy73',
    'https://www.tophattwaffle.com/hammer-tutorial-73-info_particle_systems/');
insert into SearchDataResults
values (
    'legacy74',
    'https://www.tophattwaffle.com/hammer-tutorial-74-advanced-lighting/');
insert into SearchDataResults
values (
    'legacy75',
    'https://www.tophattwaffle.com/hammer-tutorial-75-css-opening-view-and-text/');
insert into SearchDataResults
values (
    'legacy76',
    'https://www.tophattwaffle.com/hammer-tutorial-76-invisible-players-and-weapons/');
insert into SearchDataResults
values (
    'legacy77',
    'https://www.tophattwaffle.com/hammer-tutorial-77-swinging-objects-phys_ballsocket/');
insert into SearchDataResults
values (
    'legacy78',
    'https://www.tophattwaffle.com/hammer-tutorial-78-multi-floor-elevator/');
insert into SearchDataResults
values (
    'legacy79',
    'https://www.tophattwaffle.com/hammer-tutorial-79-real-time-mirrors/');
insert into SearchDataResults
values (
    'legacy80',
    'https://www.tophattwaffle.com/hammer-tutorial-80-blowing-up-a-wall-with-gibs/');
insert into SearchDataResults
values (
    'legacy81',
    'https://www.tophattwaffle.com/hammer-tutorial-81-mouse-controlled-turret/');
insert into SearchDataResults
values (
    'legacy82',
    'https://www.tophattwaffle.com/hammer-tutorial-82-random-teleport/');
insert into SearchDataResults
values (
    'legacy83',
    'https://www.tophattwaffle.com/hammer-tutorial-83-random-code-key-pad/');
insert into SearchDataResults
values (
    'legacy84',
    'https://www.tophattwaffle.com/hammer-tutorial-84-team-based-buttons/');
insert into SearchDataResults
values (
    'legacy85',
    'https://www.tophattwaffle.com/hammer-tutorial-85-parent-attachment-points-panels-from-portal-2/');
insert into SearchDataResults
values (
    'legacy86',
    'https://www.tophattwaffle.com/hammer-tutorial-86-instances-portal-2-l4d2-alien-swarm/');
insert into SearchDataResults
values (
    'legacy87',
    'https://www.tophattwaffle.com/hammer-tutorial-87-portal-2-buttons-and-indicator-lights/');
insert into SearchDataResults
values (
    'legacy88',
    'https://www.tophattwaffle.com/hammer-tutorial-88-portal-2-info-signs/');
insert into SearchDataResults
values (
    'legacy89',
    'https://www.tophattwaffle.com/hammer-tutorial-89-portal-2-world-portals/');
insert into SearchDataResults
values (
    'legacy90',
    'https://www.tophattwaffle.com/hammer-tutorial-90-portal-2-arrivaldeparture-elevators-elevator-movies-and-linking-maps/');
insert into SearchDataResults
values (
    'legacy91',
    'https://www.tophattwaffle.com/hammer-tutorial-91-creating-a-soundscape-and-looping-a-wav/');
insert into SearchDataResults
values (
    'legacy92',
    'https://www.tophattwaffle.com/hammer-tutorial-92-placing-portals-in-hammer/');
insert into SearchDataResults
values ('legacy93', 'https://www.tophattwaffle.com/hammer-tutorial-93-fog/');
insert into SearchDataResults
values (
    'legacy94',
    'https://www.tophattwaffle.com/hammer-tutorial-94-info_lightinglighting-origin/');
insert into SearchDataResults
values (
    'legacy95',
    'https://www.tophattwaffle.com/hammer-tutorial-95-dynamic-lighting-env_projectedtexture/');
insert into SearchDataResults
values ('legacy96', 'https://www.tophattwaffle.com/hammer-tutorial-96-rivers/');
insert into SearchDataResults
values (
    'legacy97',
    'https://www.tophattwaffle.com/hammer-tutorial-97-in-game-messaging/');
insert into SearchDataResults
values (
    'legacy98',
    'https://www.tophattwaffle.com/hammer-tutorial-98-breaking-a-light/');
insert into SearchDataResults
values (
    'legacy99',
    'https://www.tophattwaffle.com/hammer-tutorial-99-texture-manipulation/');
insert into SearchDataResults
values (
    'legacy100',
    'https://www.tophattwaffle.com/hammer-tutorial-100-creating-and-editing-particle-systems/');

commit;
