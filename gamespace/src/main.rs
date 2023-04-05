use std::env;
use std::fs;
use std::path::Path;

const CLEANUP_ARGUMENT: &str = "--cleanup";

fn main() {
    let args: Vec<String> = env::args().collect();
    let mut exception_messages: Vec<String> = Vec::new();
    if should_cleanup_orphaned_directory(&args, &mut exception_messages) {
        return;
    }
}

fn get_game_root_directories() -> Vec<&'static str> {
    vec![
        "D:\\Battle_Net",
        "D:\\EA_Games",
        "D:\\EpicGames",
        "D:\\GOG_Galaxy\\Games",
        "D:\\SteamLibrary\\steamapps\\common",
        "D:\\Ubisoft",
        "D:\\XboxGames",
    ]
}

fn should_cleanup_orphaned_directory(args: &[String], exception_messages: &mut Vec<String>) -> bool {
    if !args.contains(&String::from(CLEANUP_ARGUMENT)) {
        return false;
    }

    let game_folder_to_delete = args
        .iter()
        .filter(|a| *a != CLEANUP_ARGUMENT)
        .collect::<Vec<&String>>()
        .join(" "); // Join elements of Vec with space separator

    for directory in get_game_root_directories() {
        if !Path::new(directory).exists() {
            continue;
        }

        let dirs = fs::read_dir(directory)
            .expect("Failed to read directory")
            .map(|entry| entry.expect("Failed to read entry").path())
            .filter(|path| path.is_dir() && path.to_string_lossy().contains(&game_folder_to_delete))
            .collect::<Vec<_>>();

        if dirs.is_empty() {
            continue;
        }

        if dirs.len() > 1 {
            println!(
                "Found more than 1 directory matching {} in {}...",
                game_folder_to_delete, directory
            );
            continue;
        }

        let found_directory = &dirs[0];
        let exe_files = fs::read_dir(found_directory)
            .expect("Failed to read directory")
            .map(|entry| entry.expect("Failed to read entry").path())
            .filter(|path| path.is_file() && path.extension().unwrap_or_default() == "exe")
            .collect::<Vec<_>>();

        if !exe_files.is_empty() {
            println!(
                "Some executable files were found. The directory {} may not be safe to remove...",
                found_directory.display()
            );
            break;
        }

        println!("Removing {} directory...", found_directory.display());
        fs::remove_dir_all(found_directory).expect("Failed to remove directory");
        break;
    }

    true
}
