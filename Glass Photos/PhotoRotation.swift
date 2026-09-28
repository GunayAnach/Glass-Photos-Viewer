struct PhotoRotation: Equatable {
    private(set) var quarterTurns = 0

    var degrees: Int { quarterTurns * 90 }

    mutating func rotateClockwise() {
        quarterTurns = (quarterTurns + 1) % 4
    }

    mutating func rotateCounterClockwise() {
        quarterTurns = (quarterTurns + 3) % 4
    }

    mutating func reset() {
        quarterTurns = 0
    }
}
