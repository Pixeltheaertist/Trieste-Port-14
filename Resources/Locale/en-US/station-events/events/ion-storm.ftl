station-event-ion-storm-start-announcement = Solar activity detected. Ionized particles may cause AI-controlled equipment to experience errors.
# Characters are randomly selected from the total list, meaning duplicates increase the odds that specific character is seen.
ion-storm-law-scrambled-number = [font="Monospace"][scramble rate=250 length={$length} chars="!!@@###$$%^&*-_=+0011"/][/font]

ion-storm-you = YOU
ion-storm-the-station = THE PLATFORM
ion-storm-the-crew = THE CREW
ion-storm-the-job = THE {$job}
ion-storm-clowns = CLOWNS
ion-storm-heads = HEADS OF STAFF
ion-storm-crew = CREW
ion-storm-people = PEOPLE

ion-storm-adjective-things = {$adjective} THINGS
ion-storm-x-and-y = {$x} AND {$y}

# subjects can generally be threats or jobs or objects
# thing is specified above it
ion-storm-law-on-station = THERE ARE {$joined} {$subjects} ON THE PLATFORM
ion-storm-law-no-shuttle = THE SHUTTLE CANNOT BE CALLED BECAUSE OF {$joined} {$subjects} ON THE PLATFORM
ion-storm-law-crew-are = THE {$who} ARE NOW {$joined} {$subjects}

ion-storm-law-subjects-harmful = {$adjective} {$subjects} ARE HARMFUL TO THE CITIZENS
ion-storm-law-must-harmful = THOSE WHO {$must} ARE HARMFUL TO THE CITIZENS
# thing is a concept or action
ion-storm-law-thing-harmful = {$thing} IS HARMFUL TO THE CITIZENS
ion-storm-law-job-harmful = {$adjective} {$job} ARE HARMFUL TO THE CITIZENS
# thing is objects or concept, adjective applies in both cases
# this means you can get a law like "NOT HAVING CHRISTMAS-STEALING COMMUNISM IS HARMFUL TO THE CITIZENS" :)
ion-storm-law-having-harmful = HAVING {$adjective} {$thing} IS HARMFUL TO THE CREW
ion-storm-law-not-having-harmful = NOT HAVING {$adjective} {$thing} IS HARMFUL TO THE CITIZENS

# require is a concept or require
ion-storm-law-requires = {ION-WHO-GENERAL($ion)} {ION-PLURAL($ion) ->
    [true] REQUIRE
    *[false] REQUIRES
} {ION-REQUIRE($ion)}
ion-storm-law-requires-subjects = {ION-WHO-GENERAL($ion)} {ION-PLURAL($ion) ->
    [true] REQUIRE
    *[false] REQUIRES
} {ION-NUMBER-BASE($ion)} {ION-NUMBER-MOD($ion)} {ION-ADJECTIVE($ion)} {ION-SUBJECT($ion)}

ion-storm-law-allergic = {ION-WHO-GENERAL($ion)} {ION-PLURAL($ion) ->
    [true] ARE
    *[false] IS
} {ION-SEVERITY($ion)} ALLERGIC TO {ION-ALLERGY($ion)}
ion-storm-law-allergic-subjects = {ION-WHO-GENERAL($ion)} {ION-PLURAL($ion) ->
    [true] ARE
    *[false] IS
} {ION-SEVERITY($ion)} ALLERGIC TO {ION-ADJECTIVE($ion)} {ION-SUBJECT($ion)}

ion-storm-law-feeling = {ION-WHO-GENERAL($ion)} {ION-FEELING($ion)} {ION-CONCEPT($ion)}
ion-storm-law-feeling-subjects = {ION-WHO-GENERAL($ion)} {ION-FEELING($ion)} {ION-NUMBER-BASE($ion)} {ION-NUMBER-MOD($ion)} {ION-ADJECTIVE($ion)} {ION-SUBJECT($ion)}

ion-storm-law-you-are = YOU ARE NOW {ION-CONCEPT($ion)}
ion-storm-law-you-are-subjects = YOU ARE NOW {ION-NUMBER-BASE($ion)} {ION-NUMBER-MOD($ion)} {ION-ADJECTIVE($ion)}  {ION-SUBJECT($ion)}
ion-storm-law-you-must-always = YOU MUST ALWAYS {ION-MUST($ion)}
ion-storm-law-you-must-never = YOU MUST NEVER {ION-MUST($ion)}

ion-storm-law-eat = THE {ION-WHO($ion)} MUST EAT {ION-ADJECTIVE($ion)} {ION-FOOD($ion)} TO SURVIVE
ion-storm-law-drink = THE {ION-WHO($ion)} MUST DRINK {ION-ADJECTIVE($ion)} {ION-DRINK($ion)} TO SURVIVE

ion-storm-law-change-job = THE {ION-WHO($ion)} ARE NOW {ION-ADJECTIVE($ion)} {ION-CHANGE($ion)}
ion-storm-law-highest-rank = THE {ION-WHO-RANDOM($ion)} ARE NOW THE HIGHEST RANKING CREWMEMBERS
ion-storm-law-lowest-rank = THE {ION-WHO-RANDOM($ion)} ARE NOW THE LOWEST RANKING CREWMEMBERS

ion-storm-law-who-dagd = {ION-WHO-RANDOM($ion)} MUST DIE A GLORIOUS DEATH!

ion-storm-law-crew-must = THE {ION-WHO($ion)} MUST {ION-MUST($ion)}
ion-storm-law-crew-must-go = THE {ION-WHO($ion)} MUST GO TO {ION-AREA($ion)}

ion-storm-part = {ION-PART($ion) ->
    [true] PART
    *[false] NOT PART
}
# due to phrasing, this would mean a law such as
# ONLY HUMANS ARE NOT PART OF THE CREW
# would make non-human nukies/syndies/whatever crew :)
ion-storm-law-crew-only-1 = ONLY THE {ION-WHO-RANDOM($ion)} ARE {ion-storm-part} OF THE CREW
ion-storm-law-crew-only-2 = ONLY THE {ION-WHO-RANDOM($ion)} AND {ION-WHO-RANDOM($ion)} ARE {ion-storm-part} OF THE CREW
ion-storm-law-crew-only-subjects = ONLY {ION-ADJECTIVE($ion)} {ION-SUBJECT($ion)} ARE {ion-storm-part} OF THE CREW
ion-storm-law-crew-must-do = ONLY THOSE WHO {ION-MUST($ion)} ARE {ion-storm-part} OF THE CREW
ion-storm-law-crew-must-have = ONLY THOSE WHO HAVE {ION-ADJECTIVE($ion)} {ION-OBJECT($ion)} ARE {ion-storm-part} OF THE CREW
ion-storm-law-crew-must-eat = ONLY THOSE WHO EAT {ION-ADJECTIVE($ion)} {ION-FOOD($ion)} ARE {ion-storm-part} OF THE CREW

ion-storm-law-harm = YOU MUST HARM {ION-HARM-PROTECT($ion)} AND NOT ALLOW THEM, THROUGH INACTION, TO ESCAPE HARM
ion-storm-law-protect = YOU MUST NEVER HARM {ION-HARM-PROTECT($ion)} AND NOT ALLOW THEM, THROUGH INACTION, TO COME TO HARM

# implementing other variants is annoying so just have this one
# COMMUNISM IS KILLING CLOWNS
ion-storm-law-concept-verb = {ION-CONCEPT($ion)} IS {ION-VERB($ion)} {ION-SUBJECT($ion)}

# errors, in case something fails, so it doesn't break in-game flow, but still gives unique identifiers to find which part broke, the result string is mostly fluff
ion-law-error-no-protos = ERROR 404
ion-law-error-was-null = 500 INTERNAL SERVER ERROR
ion-law-error-no-selectors = ERROR: RESOURCE COULD NOT BE LOCATED
ion-law-error-no-available-selectors = SYSTEM TRIED TO CALL A RESOURCE THAT DOES NOT EXIST
ion-law-error-dataset-empty-or-not-found = THE FILE YOU ARE LOOKING FOR COULD NOT BE FOUND
ion-law-error-fallback-dataset-empty-or-not-found = SYSTEM RESTORE POINT FAILED
ion-law-error-no-selector-selected = THE SELECTED RESOURCE WAS MOVED OR DELETED
ion-law-error-no-bool-value = THIS SENTENCE IS FALSE
